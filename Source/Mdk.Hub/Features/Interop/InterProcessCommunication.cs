using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Storage;

namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Handles inter-process communication between CommandLine and Hub using TCP sockets.
///     Implements single-instance pattern using TCP port binding.
/// </summary>
[Singleton<IInterProcessCommunication>]
public class InterProcessCommunication : IInterProcessCommunication
{
    const string PortFileName = "hub-ipc.port";
    const string StartupLockFileName = "hub-ipc.starting";
    static readonly TimeSpan StartupPollDelay = TimeSpan.FromMilliseconds(100);

    readonly bool _createdNew;
    readonly IFileStorageService _fileStorage;
    readonly ILogger _logger;
    readonly int _port;
    readonly CancellationTokenSource? _serverCancellation;
    readonly ISettings _settings;
    TcpListener? _listener;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InterProcessCommunication" /> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileStorage">The file storage service.</param>
    /// <param name="settings">The settings service.</param>
    public InterProcessCommunication(ILogger logger, IFileStorageService fileStorage, ISettings settings)
    {
        _logger = logger;
        _fileStorage = fileStorage;
        _settings = settings;

        if (TryUseRunningInstance(out var existingPort))
        {
            _createdNew = false;
            _port = existingPort;
            return;
        }

        while (true)
        {
            if (!TryAcquireStartupLock(out var startupLock))
            {
                _logger.Debug("Another Hub instance is starting; waiting for the IPC endpoint");
                Thread.Sleep(StartupPollDelay);
                continue;
            }

            if (startupLock is null)
                continue;

            try
            {
                if (TryUseRunningInstance(out existingPort))
                {
                    _createdNew = false;
                    _port = existingPort;
                    return;
                }

                _port = DeterminePort();
                _createdNew = true;
                _logger.Info("Hub is the first instance - starting IPC server");
                _serverCancellation = new CancellationTokenSource();
                StartListening(startupLock, _serverCancellation.Token);
                startupLock = null;
                return;
            }
            finally
            {
                startupLock?.Dispose();
            }
        }
    }

    /// <summary>
    ///     Gets a value indicating whether another instance of the Hub is already running.
    /// </summary>
    /// <returns><c>true</c> if another instance is running; otherwise, <c>false</c>.</returns>
    public bool IsAlreadyRunning() => !_createdNew;

    /// <summary>
    ///     Occurs when an IPC message is received from a client.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     Submits a message to the IPC system, either handling it locally or forwarding to the running instance.
    /// </summary>
    /// <param name="message">The message to submit.</param>
    public async Task SubmitAsync(InterConnectMessage message)
    {
        if (_createdNew)
        {
            _logger.Debug("Handling message locally (we are the server)");
            OnMessageReceived(message);
            return;
        }

        try
        {
            var portFilePath = GetPortFilePath();
            _logger.Debug($"Looking for port file at: {portFilePath}");

            if (!_fileStorage.FileExists(portFilePath))
            {
                _logger.Error("Hub IPC port file not found - cannot send message");
                return;
            }

            var portText = await _fileStorage.ReadAllTextAsync(portFilePath);
            if (!int.TryParse(portText, out var port))
            {
                _logger.Error($"Invalid port in IPC file: {portText}");
                return;
            }

            _logger.Debug($"Connecting to Hub on port {port}...");
            using var client = new TcpClient();

            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            if (await Task.WhenAny(connectTask, Task.Delay(5000)) != connectTask)
            {
                _logger.Error("Connection to Hub timed out after 5 seconds");
                return;
            }

            _logger.Debug("Connected to Hub, sending message...");
            await using var stream = client.GetStream();
            message.Write(stream);

            _logger.Info($"Sent {message.Type} message to Hub");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to send IPC message: {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Releases all resources used by the <see cref="InterProcessCommunication" /> instance.
    /// </summary>
    public void Dispose()
    {
        _serverCancellation?.Cancel();
        _listener?.Stop();
        StartupLockState.ReleaseHeldLock();
        _serverCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }

    bool TryConnect(int port)
    {
        try
        {
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
            if (Task.WhenAny(connectTask, Task.Delay(1000)).Result == connectTask)
                return connectTask.IsCompletedSuccessfully;
            return false;
        }
        catch
        {
            return false;
        }
    }

    int DeterminePort()
    {
        var portFilePath = GetPortFilePath();
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var configuredPort = hubSettings.IpcPort;

        if (configuredPort.HasValue && configuredPort.Value > 0)
        {
            _logger.Info($"Using configured IPC port: {configuredPort.Value}");
            return configuredPort.Value;
        }

        if (!_fileStorage.FileExists(portFilePath))
        {
            _logger.Debug("No port file found, using random port");
            return 0;
        }

        try
        {
            var portText = _fileStorage.ReadAllText(portFilePath);
            if (int.TryParse(portText, out var reusablePort))
            {
                _logger.Info($"Reusing port from file: {reusablePort}");
                return reusablePort;
            }
        }
        catch (IOException ex)
        {
            _logger.Debug($"Could not read port file while selecting port: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.Debug($"Could not access port file while selecting port: {ex.Message}");
        }

        _logger.Debug("Port file invalid, using random port");
        return 0;
    }

    void StartListening(FileStream startupLock, CancellationToken cancellationToken)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();

            var actualPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _logger.Info($"IPC server listening on port {actualPort}");

            var portFilePath = GetPortFilePath();
            var portDir = Path.GetDirectoryName(portFilePath);
            if (!string.IsNullOrEmpty(portDir))
                _fileStorage.CreateDirectory(portDir);
            _fileStorage.WriteAllText(portFilePath, actualPort.ToString());

            startupLock.Dispose();
            _ = AcceptConnectionsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _listener?.Stop();
            _listener = null;
            _logger.Error($"IPC server error: {ex.Message}", ex);
            throw;
        }
    }

    async Task AcceptConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_listener is not null && !cancellationToken.IsCancellationRequested)
            {
                var client = await _listener.AcceptTcpClientAsync(cancellationToken);
                _logger.Debug("Accepted TCP connection from client");
                _ = HandleConnectionAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("IPC server shutting down");
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.Debug("IPC server shutting down");
        }
        catch (Exception ex)
        {
            _logger.Error($"IPC server error: {ex.Message}", ex);
        }
        finally
        {
            _listener?.Stop();
            _listener = null;
            CleanupPortFile();
        }
    }

    async Task HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                await using var stream = client.GetStream();
                var message = InterConnectMessage.Read(stream);

                _logger.Info($"Received {message.Type} message from client");

                if (Dispatcher.UIThread.CheckAccess())
                    OnMessageReceived(message);
                else
                    await Dispatcher.UIThread.InvokeAsync(() => OnMessageReceived(message));
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling IPC connection: {ex.Message}", ex);
        }
    }

    void OnMessageReceived(InterConnectMessage message) => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));

    void CleanupPortFile()
    {
        try
        {
            var portFilePath = GetPortFilePath();
            if (_fileStorage.FileExists(portFilePath))
                _fileStorage.DeleteFile(portFilePath);
        }
        catch (Exception ex)
        {
            // Best-effort cleanup: log and continue; a stale port file may cause confusing behavior later.
            _logger.Warning($"Failed to clean up IPC port file: {ex.Message}");
        }
    }

    bool TryAcquireStartupLock(out FileStream? startupLock)
    {
        startupLock = StartupLockState.TakeHeldLock();
        if (startupLock is not null)
            return true;

        var startupLockPath = GetStartupLockPath();
        var startupLockDir = Path.GetDirectoryName(startupLockPath);
        if (!string.IsNullOrEmpty(startupLockDir))
            _fileStorage.CreateDirectory(startupLockDir);

        startupLock = TryOpenStartupLock(startupLockPath);
        return startupLock is not null;
    }

    bool TryUseRunningInstance(out int existingPort)
    {
        existingPort = 0;
        if (!TryReadRunningPort(GetPortFilePath(), _fileStorage.FileExists, _fileStorage.ReadAllText, TryConnect, out existingPort))
            return false;

        _logger.Info($"Hub is already running on port {existingPort}");
        return true;
    }

    string GetPortFilePath() => Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub", PortFileName);
    string GetStartupLockPath() => Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub", StartupLockFileName);

    static string GetStandalonePortFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "MDK2", "Hub", PortFileName);
    }

    static string GetStandaloneStartupLockPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "MDK2", "Hub", StartupLockFileName);
    }

    static bool TryReadRunningPort(string portFilePath, Func<string, bool> fileExists, Func<string, string> readAllText, Func<int, bool> tryConnect, out int existingPort)
    {
        existingPort = 0;

        try
        {
            if (!fileExists(portFilePath))
                return false;

            var portText = readAllText(portFilePath);
            if (!int.TryParse(portText, out var candidatePort))
                return false;

            if (!tryConnect(candidatePort))
                return false;

            existingPort = candidatePort;
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    static FileStream? TryOpenStartupLock(string startupLockPath)
    {
        try
        {
            return new FileStream(startupLockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    static class StartupLockState
    {
        static readonly object Sync = new();
        static FileStream? _heldLock;

        public static void Hold(FileStream startupLock)
        {
            lock (Sync)
                _heldLock = startupLock;
        }

        public static FileStream? TakeHeldLock()
        {
            lock (Sync)
            {
                var startupLock = _heldLock;
                _heldLock = null;
                return startupLock;
            }
        }

        public static void ReleaseHeldLock()
        {
            lock (Sync)
            {
                _heldLock?.Dispose();
                _heldLock = null;
            }
        }
    }

    /// <summary>
    ///     Standalone IPC helper for use in Program.Main before DI is available.
    /// </summary>
    public class Standalone : IDisposable
    {
        readonly bool _isFirstInstance;
        readonly ILogger _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="Standalone" /> class.
        /// </summary>
        public Standalone()
        {
            var fileStorage = new FileStorageService();
            _logger = new FileLogger(fileStorage);

            while (true)
            {
                if (TryReadRunningPort(GetStandalonePortFilePath(), File.Exists, File.ReadAllText, TryConnect, out var port))
                {
                    _isFirstInstance = false;
                    _logger.Info($"Found running instance on port {port}");
                    return;
                }

                var startupLockPath = GetStandaloneStartupLockPath();
                var startupLockDir = Path.GetDirectoryName(startupLockPath);
                if (!string.IsNullOrEmpty(startupLockDir))
                    Directory.CreateDirectory(startupLockDir);

                var startupLock = TryOpenStartupLock(startupLockPath);
                if (startupLock is null)
                {
                    _logger.Debug("Another Hub instance is starting; waiting for the IPC endpoint");
                    Thread.Sleep(StartupPollDelay);
                    continue;
                }

                if (TryReadRunningPort(GetStandalonePortFilePath(), File.Exists, File.ReadAllText, TryConnect, out port))
                {
                    startupLock.Dispose();
                    _isFirstInstance = false;
                    _logger.Info($"Found running instance on port {port}");
                    return;
                }

                StartupLockState.Hold(startupLock);
                _isFirstInstance = true;
                _logger.Info("Acquired Hub startup lock");
                return;
            }
        }

        /// <summary>
        ///     Releases all resources used by the <see cref="Standalone" /> instance.
        /// </summary>
        public void Dispose()
        {
            StartupLockState.ReleaseHeldLock();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets a value indicating whether another instance of the Hub is already running.
        /// </summary>
        /// <returns><c>true</c> if another instance is running; otherwise, <c>false</c>.</returns>
        public bool IsAlreadyRunning() => !_isFirstInstance;

        static bool TryConnect(int port)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
                if (Task.WhenAny(connectTask, Task.Delay(1000)).Result == connectTask)
                    return connectTask.IsCompletedSuccessfully;
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Sends an IPC message to the running Hub instance based on command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments containing the message type and data.</param>
        public void SendMessage(string[] args)
        {
            try
            {
                _logger.Info($"Standalone.SendMessage called with {args.Length} args: {string.Join(" ", args)}");

                if (args.Length < 1)
                {
                    _logger.Warning($"Invalid arguments: {string.Join(" ", args)}");
                    return;
                }

                var isKnownNotification = Enum.TryParse<NotificationType>(args[0], true, out var type) && type != NotificationType.StartupArgs;
                var messageArgs = isKnownNotification ? args.Skip(1).ToArray() : args;
                type = isKnownNotification ? type : NotificationType.StartupArgs;
                var message = new InterConnectMessage(type, messageArgs);

                var portFilePath = GetStandalonePortFilePath();
                if (!File.Exists(portFilePath))
                {
                    _logger.Error($"Port file not found: {portFilePath}");
                    return;
                }

                var portText = File.ReadAllText(portFilePath);
                if (!int.TryParse(portText, out var port))
                {
                    _logger.Error($"Invalid port in file: {portText}");
                    return;
                }

                _logger.Debug($"Connecting to Hub on port {port}...");

                using var client = new TcpClient();
                client.Connect(IPAddress.Loopback, port);

                using var stream = client.GetStream();
                message.Write(stream);

                _logger.Info($"Sent {type} message successfully to Hub");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to send IPC message", ex);
            }
        }
    }
}

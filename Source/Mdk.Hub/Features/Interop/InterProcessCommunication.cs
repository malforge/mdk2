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
    readonly bool _createdNew;

    readonly IFileStorageService _fileStorage;
    readonly ILogger _logger;
    readonly ISettings _settings;
    readonly CancellationTokenSource? _serverCancellation;
    readonly int _port;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InterProcessCommunication"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="fileStorage">The file storage service.</param>
    /// <param name="settings">The settings service.</param>
    public InterProcessCommunication(ILogger logger, IFileStorageService fileStorage, ISettings settings)
    {
        _logger = logger;
        _fileStorage = fileStorage;
        _settings = settings;

        // Check if another instance is already running by checking the port file
        var portFilePath = GetPortFilePath();
        if (_fileStorage.FileExists(portFilePath))
        {
            var portText = _fileStorage.ReadAllText(portFilePath);
            if (int.TryParse(portText, out var existingPort))
            {
                _logger.Debug($"Found existing port file with port {existingPort}, checking if instance is alive");
                
                // Try connecting to verify the instance is still running
                if (TryConnect(existingPort))
                {
                    _logger.Info($"Hub is already running on port {existingPort}");
                    _createdNew = false;
                    _port = existingPort;
                    return;
                }
                
                _logger.Warning($"Port file exists but no instance running on port {existingPort}, starting as first instance");
            }
        }

        // We're the first instance - determine port to use
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var configuredPort = hubSettings.IpcPort;

        if (configuredPort.HasValue && configuredPort.Value > 0)
        {
            // User configured a specific port
            _port = configuredPort.Value;
            _logger.Info($"Using configured IPC port: {_port}");
        }
        else if (_fileStorage.FileExists(portFilePath))
        {
            // Try to reuse the port from the file
            var portText = _fileStorage.ReadAllText(portFilePath);
            if (int.TryParse(portText, out var reusablePort))
            {
                _port = reusablePort;
                _logger.Info($"Reusing port from file: {_port}");
            }
            else
            {
                _port = 0; // Random port
                _logger.Debug("Port file invalid, using random port");
            }
        }
        else
        {
            _port = 0; // Random port
            _logger.Debug("No port file found, using random port");
        }

        _createdNew = true;
        _logger.Info("Hub is the first instance - starting IPC server");
        _serverCancellation = new CancellationTokenSource();
        StartListeningAsync(_serverCancellation.Token);
    }

    /// <summary>
    /// Gets a value indicating whether another instance of the Hub is already running.
    /// </summary>
    /// <returns><c>true</c> if another instance is running; otherwise, <c>false</c>.</returns>
    public bool IsAlreadyRunning() => !_createdNew;

    /// <summary>
    /// Occurs when an IPC message is received from a client.
    /// </summary>
    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    /// Submits a message to the IPC system, either handling it locally or forwarding to the running instance.
    /// </summary>
    /// <param name="message">The message to submit.</param>
    public async Task SubmitAsync(InterConnectMessage message)
    {
        if (_createdNew)
        {
            // We are the server - handle the message directly
            _logger.Debug("Handling message locally (we are the server)");
            OnMessageReceived(message);
            return;
        }

        // We are a client - send to the server
        try
        {
            var portFilePath = GetPortFilePath();
            _logger.Debug($"Looking for port file at: {portFilePath}");

            if (!File.Exists(portFilePath))
            {
                _logger.Error("Hub IPC port file not found - cannot send message");
                return;
            }

            var portText = await File.ReadAllTextAsync(portFilePath);
            if (!int.TryParse(portText, out var port))
            {
                _logger.Error($"Invalid port in IPC file: {portText}");
                return;
            }

            _logger.Debug($"Connecting to Hub on port {port}...");
            using var client = new TcpClient();

            // Add timeout for connection
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
    /// Releases all resources used by the <see cref="InterProcessCommunication"/> instance.
    /// </summary>
    public void Dispose()
    {
        _serverCancellation?.Cancel();
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
            {
                return connectTask.IsCompletedSuccessfully;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    async void StartListeningAsync(CancellationToken cancellationToken)
    {
        TcpListener? listener = null;
        try
        {
            // Start TCP listener on the determined port
            listener = new TcpListener(IPAddress.Loopback, _port);
            listener.Start();

            var actualPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            _logger.Info($"IPC server listening on port {actualPort}");

            // Save port to file for clients to discover
            var portFilePath = GetPortFilePath();
            var portDir = Path.GetDirectoryName(portFilePath);
            if (!string.IsNullOrEmpty(portDir))
                _fileStorage.CreateDirectory(portDir);
            await _fileStorage.WriteAllTextAsync(portFilePath, actualPort.ToString(), cancellationToken);

            // Accept connections
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _logger.Debug("Accepted TCP connection from client");
                _ = HandleConnectionAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.Debug("IPC server shutting down");
        }
        catch (Exception ex)
        {
            _logger.Error($"IPC server error: {ex.Message}");
        }
        finally
        {
            listener?.Stop();

            // Clean up port file
            try
            {
                var portFilePath = GetPortFilePath();
                if (_fileStorage.FileExists(portFilePath))
                    _fileStorage.DeleteFile(portFilePath);
            }
            catch
            {
                // Best effort cleanup
            }
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

                // Raise event on UI thread if available
                if (Dispatcher.UIThread.CheckAccess())
                    OnMessageReceived(message);
                else
                    await Dispatcher.UIThread.InvokeAsync(() => OnMessageReceived(message));
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger.Error($"Error handling IPC connection: {ex.Message}");
        }
    }

    void OnMessageReceived(InterConnectMessage message) => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(message));

    string GetPortFilePath() => Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub", PortFileName);
    
    static string GetStandalonePortFilePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appDataPath, "MDK2", "Hub", PortFileName);
    }

    /// <summary>
    ///     Standalone IPC helper for use in Program.Main before DI is available.
    /// </summary>
    public class Standalone : IDisposable
    {
        readonly bool _isFirstInstance;
        readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="Standalone"/> class.
        /// </summary>
        public Standalone()
        {
            var fileStorage = new FileStorageService(); // Use production implementation for startup
            _logger = new FileLogger(fileStorage);
            
            // Check if another instance is running by checking port file
            var portFilePath = GetStandalonePortFilePath();
            if (File.Exists(portFilePath))
            {
                var portText = File.ReadAllText(portFilePath);
                if (int.TryParse(portText, out var port))
                {
                    // Try to connect to verify instance is alive
                    _isFirstInstance = !TryConnect(port);
                    if (!_isFirstInstance)
                        _logger.Info($"Found running instance on port {port}");
                }
                else
                {
                    _isFirstInstance = true;
                }
            }
            else
            {
                _isFirstInstance = true;
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Standalone"/> instance.
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets a value indicating whether another instance of the Hub is already running.
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
                {
                    return connectTask.IsCompletedSuccessfully;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sends an IPC message to the running Hub instance based on command-line arguments.
        /// </summary>
        /// <param name="args">The command-line arguments containing the message type and data.</param>
        public void SendMessage(string[] args)
        {
            try
            {
                _logger.Info($"Standalone.SendMessage called with {args.Length} args: {string.Join(" ", args)}");

                // Parse arguments
                if (args.Length < 1 || !Enum.TryParse<NotificationType>(args[0], true, out var type))
                {
                    _logger.Warning($"Invalid arguments: {string.Join(" ", args)}");
                    return;
                }

                var messageArgs = args.Skip(1).ToArray();
                var message = new InterConnectMessage(type, messageArgs);

                // Read port from file (synchronous) - use static helper for standalone
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

                // Connect and send message (synchronous)
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

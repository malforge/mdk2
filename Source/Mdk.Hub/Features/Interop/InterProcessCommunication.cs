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

namespace Mdk.Hub.Features.Interop;

/// <summary>
///     Handles inter-process communication between CommandLine and Hub using TCP sockets.
///     Implements single-instance pattern with mutex.
/// </summary>
[Singleton<IInterProcessCommunication>]
public class InterProcessCommunication : IInterProcessCommunication
{
    const string MutexName = "MDK2_Hub_SingleInstance_Mutex";
    const string PortFileName = "hub-ipc.port";
    readonly bool _createdNew;

    readonly ILogger _logger;
    readonly Mutex _mutex;
    readonly CancellationTokenSource? _serverCancellation;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InterProcessCommunication"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public InterProcessCommunication(ILogger logger)
    {
        _logger = logger;
        _mutex = new Mutex(true, MutexName, out _createdNew);

        if (_createdNew)
        {
            _logger.Info("Hub is the first instance - starting IPC server");
            _serverCancellation = new CancellationTokenSource();
            StartListeningAsync(_serverCancellation.Token);
        }
        else
            _logger.Info("Hub is already running - will forward messages to existing instance");
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
        _mutex.ReleaseMutex();
        _mutex.Dispose();
        GC.SuppressFinalize(this);
    }

    async void StartListeningAsync(CancellationToken cancellationToken)
    {
        TcpListener? listener = null;
        try
        {
            // Start TCP listener on random available port
            listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();

            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            _logger.Info($"IPC server listening on port {port}");

            // Save port to file for clients to discover
            var portFilePath = GetPortFilePath();
            Directory.CreateDirectory(Path.GetDirectoryName(portFilePath)!);
            await File.WriteAllTextAsync(portFilePath, port.ToString(), cancellationToken);

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
                if (File.Exists(portFilePath))
                    File.Delete(portFilePath);
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

    static string GetPortFilePath()
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
        readonly Mutex _mutex;

        /// <summary>
        /// Initializes a new instance of the <see cref="Standalone"/> class.
        /// </summary>
        public Standalone()
        {
            _logger = new FileLogger();
            _mutex = new Mutex(true, MutexName, out _isFirstInstance);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Standalone"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (_isFirstInstance)
                _mutex.ReleaseMutex();
            _mutex.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets a value indicating whether another instance of the Hub is already running.
        /// </summary>
        /// <returns><c>true</c> if another instance is running; otherwise, <c>false</c>.</returns>
        public bool IsAlreadyRunning() => !_isFirstInstance;

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

                // Read port from file (synchronous)
                var portFilePath = GetPortFilePath();
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

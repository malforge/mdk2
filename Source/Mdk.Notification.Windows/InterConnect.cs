using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;

namespace Mdk.Notification.Windows;

/// <summary>
///     An utility to communicate between different instances of the same application.
/// </summary>
public class InterConnect : IDisposable
{
    const string MutexName = "Mdk78f80db837744f3c993188a95cb88709";
    readonly bool _createdNew;
    readonly Mutex _mutex;
    readonly CancellationTokenSource? _serverCancellation;

    /// <summary>
    ///     Creates a new instance of <see cref="InterConnect" />.
    /// </summary>
    public InterConnect()
    {
        _mutex = new Mutex(true, MutexName, out _createdNew);
        if (!_createdNew)
            return;
        _serverCancellation = new CancellationTokenSource();
        StartListening(_serverCancellation.Token);
    }

    /// <summary>
    ///     Disposes the instance.
    /// </summary>
    public void Dispose()
    {
        _serverCancellation?.Cancel();
        _mutex.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Called when a message is received.
    /// </summary>
    public event EventHandler<InterConnectMessageReceivedEventArgs>? MessageReceived;

    /// <summary>
    ///     Determines whether the application is already running.
    /// </summary>
    /// <returns></returns>
    public bool IsAlreadyRunning() => !_createdNew;

    /// <summary>
    ///     Submits a message to the running instance.
    /// </summary>
    /// <param name="scriptFolder"></param>
    public async void Submit(string scriptFolder)
    {
        if (_createdNew)
        {
            OnMessageReceived(scriptFolder);
            return;
        }

        try
        {
            var executableName = Path.GetFileNameWithoutExtension(Application.ResourceAssembly.Location);
            var iniFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), executableName, $"{executableName}.ini");
            Directory.CreateDirectory(Path.GetDirectoryName(iniFileName)!);
            var port = int.Parse(await File.ReadAllTextAsync(iniFileName));
            using var client = new TcpClient("localhost", port);
            await using var stream = client.GetStream();
            await using var writer = new StreamWriter(stream, Encoding.UTF8);
            await writer.WriteAsync(scriptFolder);
            await writer.FlushAsync();
        }
        catch
        {
            // ignored
        }
    }

    async void StartListening(CancellationToken cancellationToken = default)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var executableName = Path.GetFileNameWithoutExtension(Application.ResourceAssembly.Location);
        var iniFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), executableName, $"{executableName}.ini");
        Directory.CreateDirectory(Path.GetDirectoryName(iniFileName)!);
        await File.WriteAllTextAsync(iniFileName, port.ToString(), cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    var client = await listener.AcceptTcpClientAsync(cancellationToken);
                    HandleConnectionAsync(client, cancellationToken);
                }
                else
                    await Task.Delay(100, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            listener.Stop();
        }
    }

    async void HandleConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using (client)
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var folder = await reader.ReadLineAsync(cancellationToken);
                if (folder == null || !Directory.Exists(folder))
                    return;
                Application.Current.Dispatcher.Invoke(() => OnMessageReceived(folder));
            }
        }
        catch (OperationCanceledException) { }
    }

    void OnMessageReceived(string folder) => MessageReceived?.Invoke(this, new InterConnectMessageReceivedEventArgs(folder));
}
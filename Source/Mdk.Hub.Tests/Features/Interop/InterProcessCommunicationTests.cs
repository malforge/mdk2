using System.IO;
using System.Net;
using System.Net.Sockets;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Interop;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Storage;

namespace Mdk.Hub.Tests.Features.Interop;

[TestFixture]
public class InterProcessCommunicationTests
{
    ILogger _logger = null!;
    ISettings _settings = null!;
    TemporaryFileStorageService _fileStorage = null!;

    [SetUp]
    public void Setup()
    {
        _logger = A.Fake<ILogger>();
        _settings = A.Fake<ISettings>();
        _fileStorage = new TemporaryFileStorageService();
        A.CallTo(() => _settings.GetValue(SettingsKeys.HubSettings, A<HubSettings>._)).Returns(new HubSettings());
    }

    [TearDown]
    public void TearDown() => _fileStorage.Dispose();

    [Test]
    public void Constructor_WhenNoInstanceIsRunning_StartsServerAndPublishesPortFile()
    {
        using var first = new InterProcessCommunication(_logger, _fileStorage, _settings);

        Assert.That(first.IsAlreadyRunning(), Is.False);

        var portFilePath = GetPortFilePath();
        Assert.That(File.Exists(portFilePath), Is.True);

        var portText = File.ReadAllText(portFilePath);
        Assert.That(int.TryParse(portText, out var port), Is.True);

        using var probe = new TcpClient();
        probe.Connect(IPAddress.Loopback, port);

        using var second = new InterProcessCommunication(_logger, _fileStorage, _settings);
        Assert.That(second.IsAlreadyRunning(), Is.True);
    }

    [Test]
    public async Task Constructor_WhenAnotherInstanceIsStarting_WaitsForItsEndpoint()
    {
        var hubDirectory = Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub");
        Directory.CreateDirectory(hubDirectory);

        var startupLockPath = Path.Combine(hubDirectory, "hub-ipc.starting");
        var portFilePath = GetPortFilePath();

        using var startupLock = new FileStream(startupLockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        using var published = new CancellationTokenSource();

        var publishTask = Task.Run(async () =>
        {
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            await Task.Delay(250);
            File.WriteAllText(portFilePath, port.ToString());
            startupLock.Dispose();

            try
            {
                while (!published.IsCancellationRequested)
                {
                    using var client = await listener.AcceptTcpClientAsync(published.Token);
                }
            }
            catch (OperationCanceledException)
            {
            }
        });

        using var ipc = new InterProcessCommunication(_logger, _fileStorage, _settings);
        Assert.That(ipc.IsAlreadyRunning(), Is.True);

        published.Cancel();
        listener.Stop();
        await publishTask;
    }

    string GetPortFilePath() => Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub", "hub-ipc.port");
}

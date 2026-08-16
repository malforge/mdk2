using System.Windows.Input;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Actions.Items;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;

namespace Mdk.Hub.Tests.Features.Updates;

/// <summary>
///     Tests for the Hub update flow. Updating is a single decision: press the button, and the download installs itself
///     the next time the Hub is closed.
/// </summary>
[TestFixture]
public class UpdatesActionTests
{
    static readonly HubVersionInfo AvailableHubVersion = new()
    {
        LatestVersion = "2.3.0",
        DownloadUrl = "https://example.invalid/releases/latest"
    };

    static VersionCheckCompletedEventArgs HubUpdateFound() => new()
    {
        Packages = [],
        HubVersion = AvailableHubVersion
    };

    static UpdatesAction CreateAction(IUpdateManager updateManager, IShell shell, out Action<VersionCheckCompletedEventArgs> reportVersionCheck)
    {
        Action<VersionCheckCompletedEventArgs>? captured = null;
        A.CallTo(() => updateManager.WhenVersionCheckUpdates(A<Action<VersionCheckCompletedEventArgs>>._))
            .Invokes((Action<VersionCheckCompletedEventArgs> callback) => captured = callback);

        var action = new UpdatesAction(shell, updateManager, A.Fake<ILogger>());

        reportVersionCheck = captured ?? throw new InvalidOperationException("The action did not subscribe to version checks");
        return action;
    }

    static IUpdateManager CreateUpdateManager(bool supported = true, bool pendingInstall = false, UpdateResult? downloadResult = null)
    {
        var updateManager = A.Fake<IUpdateManager>();
        A.CallTo(() => updateManager.IsHubUpdateSupported).Returns(supported);
        A.CallTo(() => updateManager.IsHubUpdatePendingInstall).Returns(pendingInstall);
        A.CallTo(() => updateManager.DownloadHubUpdateAsync(A<IProgress<UpdateProgress>>._, A<CancellationToken>._))
            .Returns(Task.FromResult(downloadResult ?? new UpdateResult { Success = true }));
        return updateManager;
    }

    [Test]
    public void AnAvailableUpdate_IsOfferedButNotDownloadedUntilAskedFor()
    {
        var updateManager = CreateUpdateManager();
        var action = CreateAction(updateManager, A.Fake<IShell>(), out var reportVersionCheck);

        reportVersionCheck(HubUpdateFound());

        Assert.Multiple(() =>
        {
            Assert.That(action.IsHubUpdateAvailable, Is.True, "the button has to be there to be pressed");
            Assert.That(action.IsPendingInstall, Is.False);
            Assert.That(action.ShouldShow(), Is.True);
        });
        A.CallTo(() => updateManager.DownloadHubUpdateAsync(A<IProgress<UpdateProgress>>._, A<CancellationToken>._))
            .MustNotHaveHappened();
    }

    [Test]
    public void PressingUpdate_DownloadsAndLeavesNothingFurtherToDo()
    {
        // The whole point of the change: one button, and no second step afterwards.
        var updateManager = CreateUpdateManager();
        var action = CreateAction(updateManager, A.Fake<IShell>(), out var reportVersionCheck);
        reportVersionCheck(HubUpdateFound());

        ((ICommand)action.UpdateHubCommand).Execute(null);

        A.CallTo(() => updateManager.DownloadHubUpdateAsync(A<IProgress<UpdateProgress>>._, A<CancellationToken>._))
            .MustHaveHappenedOnceExactly();
        Assert.Multiple(() =>
        {
            Assert.That(action.IsPendingInstall, Is.True, "downloaded, and installing itself on close");
            Assert.That(action.IsDownloading, Is.False);
            Assert.That(action.IsHubUpdateAvailable, Is.False, "there is no second button to press");
            Assert.That(action.StatusMessage, Does.Contain("close the Hub"), "the user is told when it lands");
        });
    }

    [Test]
    public void ADownloadedUpdate_DoesNotInterruptTheSessionThatAskedForIt()
    {
        var updateManager = CreateUpdateManager();
        var shell = A.Fake<IShell>();
        var action = CreateAction(updateManager, shell, out var reportVersionCheck);
        reportVersionCheck(HubUpdateFound());

        ((ICommand)action.UpdateHubCommand).Execute(null);

        A.CallTo(() => shell.Shutdown()).MustNotHaveHappened();
        A.CallTo(() => updateManager.ApplyHubUpdateOnExit()).MustNotHaveHappened();
    }

    [Test]
    public void AnUpdateDownloadedInAnEarlierSession_IsNotOfferedAgain()
    {
        var updateManager = CreateUpdateManager(pendingInstall: true);

        var action = CreateAction(updateManager, A.Fake<IShell>(), out var reportVersionCheck);
        reportVersionCheck(HubUpdateFound());

        Assert.Multiple(() =>
        {
            Assert.That(action.IsPendingInstall, Is.True);
            Assert.That(action.IsHubUpdateAvailable, Is.False, "it is already downloaded - closing the Hub is all that is left");
        });
    }

    [Test]
    public void WhenTheInstallationCannotUpdateItself_NothingIsOffered()
    {
        var action = CreateAction(CreateUpdateManager(false), A.Fake<IShell>(), out var reportVersionCheck);

        reportVersionCheck(HubUpdateFound());

        Assert.Multiple(() =>
        {
            Assert.That(action.IsHubUpdateAvailable, Is.False);
            Assert.That(action.ShouldShow(), Is.False, "nothing can be done about it, so say nothing");
        });
    }

    [Test]
    public void AFailedDownload_IsReportedAndPutsTheButtonBack()
    {
        var updateManager = CreateUpdateManager(downloadResult: new UpdateResult { Success = false, ErrorMessage = "the network is on fire" });
        var action = CreateAction(updateManager, A.Fake<IShell>(), out var reportVersionCheck);
        reportVersionCheck(HubUpdateFound());

        ((ICommand)action.UpdateHubCommand).Execute(null);

        Assert.Multiple(() =>
        {
            Assert.That(action.IsPendingInstall, Is.False);
            Assert.That(action.StatusMessage, Does.Contain("the network is on fire"));
            Assert.That(action.IsHubUpdateAvailable, Is.True, "the same button is the retry");
        });

        ((ICommand)action.UpdateHubCommand).Execute(null);

        A.CallTo(() => updateManager.DownloadHubUpdateAsync(A<IProgress<UpdateProgress>>._, A<CancellationToken>._))
            .MustHaveHappenedTwiceExactly();
    }

    [Test]
    public void ATemplateUpdate_IsStillOfferedSeparately()
    {
        var action = CreateAction(CreateUpdateManager(), A.Fake<IShell>(), out var reportVersionCheck);

        reportVersionCheck(new VersionCheckCompletedEventArgs
        {
            Packages = [],
            TemplatePackage = new TemplateVersionInfo { LatestVersion = "2.3.0" }
        });

        Assert.Multiple(() =>
        {
            Assert.That(action.IsTemplateUpdateAvailable, Is.True);
            Assert.That(action.IsHubUpdateAvailable, Is.False);
            Assert.That(action.ShouldShow(), Is.True);
            Assert.That(action.StatusMessage, Does.Contain("Templates"));
        });
    }
}

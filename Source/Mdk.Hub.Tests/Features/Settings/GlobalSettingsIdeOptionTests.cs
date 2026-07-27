using System.Windows.Input;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;

namespace Mdk.Hub.Tests.Features.Settings;

/// <summary>
///     Tests for the "IDE opens" option, which is only meaningful when a custom IDE
///     executable has been nominated. The option must never prevent unrelated settings
///     from being saved.
/// </summary>
[TestFixture]
public class GlobalSettingsIdeOptionTests
{
    static GlobalSettingsViewModel CreateViewModel(HubSettings stored, out ISettings settings)
    {
        var fakeSettings = A.Fake<ISettings>();
        A.CallTo(() => fakeSettings.GetValue(SettingsKeys.HubSettings, A<HubSettings>._)).Returns(stored);
        settings = fakeSettings;
        return new GlobalSettingsViewModel(fakeSettings, A.Fake<IUpdateManager>(), A.Fake<IShell>(), A.Fake<ILogger>());
    }

    [Test]
    public void Save_WithNoCustomIdePath_PersistsUnrelatedSettings()
    {
        // An empty IDE path is the default, and must not block saving anything else.
        var vm = CreateViewModel(new HubSettings { CustomIdePath = "" }, out var settings);

        vm.IpcPort = "12345";
        ((ICommand)vm.SaveCommand).Execute(null);

        A.CallTo(() => settings.SetValue(SettingsKeys.HubSettings, A<HubSettings>.That.Matches(s => s.IpcPort == 12345)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void Save_WhenClearingIdePathAndTurningTheOptionOff_Persists()
    {
        var stored = new HubSettings { CustomIdePath = @"C:\some\ide.exe", IdeOpenDirectory = true };
        var vm = CreateViewModel(stored, out var settings);

        vm.IdeOpenDirectory = false;
        vm.CustomIdePath = "";
        ((ICommand)vm.SaveCommand).Execute(null);

        A.CallTo(() => settings.SetValue(SettingsKeys.HubSettings, A<HubSettings>.That.Matches(s => s.CustomIdePath == "" && !s.IdeOpenDirectory)))
            .MustHaveHappenedOnceExactly();
    }

    [Test]
    public void ValidationError_IsRaised_OnlyWhenTheOptionIsOnWithoutAnIdePath()
    {
        var vm = CreateViewModel(new HubSettings { CustomIdePath = "" }, out _);
        Assert.That(vm.IdeOpenDirectoryValidationError, Is.Null, "no error while the option is off");

        vm.IdeOpenDirectory = true;
        Assert.That(vm.IdeOpenDirectoryValidationError, Is.Not.Null.And.Not.Empty, "error once the option is on with no path");

        vm.CustomIdePath = @"C:\some\ide.exe";
        Assert.That(vm.IdeOpenDirectoryValidationError, Is.Null, "error clears once a path is supplied");
    }
}

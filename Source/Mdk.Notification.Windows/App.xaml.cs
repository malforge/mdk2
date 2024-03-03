using System.Diagnostics;
using System.IO;
using System.Windows;

namespace Mdk.Notification.Windows;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    const int EmptyGracePeriod = 1000;

    async void OnIsEmptyChanged(object? sender, EventArgs e)
    {
        if (!Toast.IsEmpty)
            return;
        await Task.Delay(EmptyGracePeriod);
        if (Toast.IsEmpty)
            Shutdown();
    }

    void App_OnStartup(object sender, StartupEventArgs e)
    {
        List<string> arguments = [..e.Args];

        if (!arguments.TryDequeue(out var scriptFolder))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_NoScriptFolderProvided);
            Shutdown();
            return;
        }

        if (!Directory.Exists(scriptFolder))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_ScriptFolderDoesNotExist);
            Shutdown();
            return;
        }

        var scriptFileName = Path.Combine(scriptFolder, "script.cs");
        if (!File.Exists(scriptFileName))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_NoScriptFile);
            Shutdown();
            return;
        }
        // var thumb = Path.Combine(scriptFolder, "thumb.png");

        string content;
        try
        {
            content = File.ReadAllText(scriptFileName);
        }
        catch (Exception exception)
        {
            MessageBox.Show(string.Format(Notification.Windows.Resources.App_OnStartup_ErrorReadingScript, exception.Message));
            Shutdown();
            return;
        }

        Toast.IsEmptyChanged += OnIsEmptyChanged;

        Toast.Show(Notification.Windows.Resources.App_OnStartup_ScriptDeployed,
            new ToastAction
            {
                Text = Notification.Windows.Resources.App_OnStartup_ShowMe,
                Action = () => OnShowMeClicked(scriptFolder),
                IsClosingAction = true
            },
            new ToastAction
            {
                Text = Notification.Windows.Resources.App_OnStartup_CopyToClipboard,
                Action = () => OnCopyToClipboardClicked(content),
                IsClosingAction = true
            });
    }

    void OnCopyToClipboardClicked(string content)
    {
        try
        {
            Clipboard.SetText(content);
            Toast.Show(Notification.Windows.Resources.App_OnCopyToClipboardClicked_CopiedToClipboard, 2000);
        }
        catch (Exception exception)
        {
            MessageBox.Show(string.Format(Notification.Windows.Resources.App_OnCopyToClipboardClicked_CopyToClipboardError, exception.Message));
        }
    }

    void OnShowMeClicked(string scriptFolder)
    {
        try
        {
            Process.Start(new ProcessStartInfo()
            {
                FileName = scriptFolder,
                UseShellExecute = true
            });
        }
        catch (Exception exception)
        {
            MessageBox.Show(string.Format(Notification.Windows.Resources.App_OnShowMeClicked_ShowMeError, exception.Message));
        }
    }
}
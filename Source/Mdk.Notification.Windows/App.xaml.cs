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
    InterConnect? _interconnect;

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _interconnect?.Dispose();
        base.OnExit(e);
    }

    async void OnIsEmptyChanged(object? sender, EventArgs e)
    {
        if (!Toast.Instance.IsEmpty)
            return;
        await Task.Delay(EmptyGracePeriod);
        if (Toast.Instance.IsEmpty)
            Shutdown();
    }

    void App_OnStartup(object sender, StartupEventArgs e)
    {
        List<string> arguments = [..e.Args];

        if (!arguments.TryDequeue(out var type) || !string.Equals(type, "script", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_InvalidType);
            Shutdown();
            return;
        }

        if (!arguments.TryDequeue(out var projectName))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_NoProjectNameProvided);
            Shutdown();
            return;
        }
        
        if (!arguments.TryDequeue(out var scriptFolder))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_NoScriptFolderProvided);
            Shutdown();
            return;
        }

        _interconnect = new InterConnect();
        _interconnect.MessageReceived += (_, args) => OnInterConnectMessageReceived(args.Message);
        _interconnect.Submit(string.Join("\0", "script", projectName, scriptFolder));
        if (_interconnect.IsAlreadyRunning())
            Shutdown();
    }

    void ShowToast(string arguments)
    {
        var parameters = arguments.Split('\0');
        if (parameters.Length != 3 || !string.Equals(parameters[0], "script", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_InvalidType);
            return;
        }
        
        var projectName = parameters[1];
        var scriptFolder = parameters[2];
        
        if (!Directory.Exists(scriptFolder))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_ScriptFolderDoesNotExist);
            return;
        }

        var scriptFileName = Path.Combine(scriptFolder, "script.cs");
        if (!File.Exists(scriptFileName))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_NoScriptFile);
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
            return;
        }

        Toast.Instance.IsEmptyChanged += OnIsEmptyChanged;

        Toast.Instance.Show(string.Format(Notification.Windows.Resources.App_OnStartup_ScriptDeployed, projectName),
            0,
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

    void OnInterConnectMessageReceived(string folder) => ShowToast(folder);

    void OnCopyToClipboardClicked(string content)
    {
        try
        {
            Clipboard.SetText(content);
            Toast.Instance.Show(Notification.Windows.Resources.App_OnCopyToClipboardClicked_CopiedToClipboard, 2000);
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
            Process.Start(new ProcessStartInfo
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
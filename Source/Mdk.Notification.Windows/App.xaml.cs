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

    static string Unescape(string value)
    {
        return value.Replace("&quot;", "\"");
    }
    
    void App_OnStartup(object sender, StartupEventArgs e)
    {
        List<string> arguments = [..e.Args];

        if (!arguments.TryDequeue(out var type) || !Enum.TryParse<NotificationType>(type, true, out var notificationType) || !Enum.IsDefined(notificationType))
        {
            MessageBox.Show(Notification.Windows.Resources.App_OnStartup_InvalidType);
            Shutdown();
            return;
        }

        for (var i = 0; i < arguments.Count; i++)
            arguments[i] = Unescape(arguments[i]);

        _interconnect = new InterConnect();
        _interconnect.MessageReceived += (_, args) => OnInterConnectMessageReceived(args.Message);
        _interconnect.Submit(new InterConnectMessage(notificationType, arguments.ToArray()));
        if (_interconnect.IsAlreadyRunning())
            Shutdown();
    }

    void ShowToast(InterConnectMessage message)
    {
        switch (message.Type)
        {
            case NotificationType.Script:
                ShowScriptToast(message.Arguments);
                break;
            case NotificationType.Nuget:
                ShowNugetPackageVersionAvailableToast(message.Arguments);
                break;
            case NotificationType.Custom:
                ShowCustomToast(message.Arguments);
                break;
            default:
                ShowCustomToast([Notification.Windows.Resources.App_OnStartup_InvalidType]);
                break;
        }
    }

    void ShowScriptToast(string[] messageArguments)
    {
        var projectName = TryGet(messageArguments, 0, string.Empty);
        var scriptFolder = TryGet(messageArguments, 1, string.Empty);
        var message = TryGet(messageArguments, 2, string.Empty);
        if (string.IsNullOrEmpty(message))
            message = string.Format(Notification.Windows.Resources.App_OnStartup_ScriptDeployed, projectName);
        
        if (!Directory.Exists(scriptFolder))
        {
            ShowCustomToast([Notification.Windows.Resources.App_OnStartup_ScriptFolderDoesNotExist]);
            return;
        }
        
        var scriptFileName = Path.Combine(scriptFolder, "script.cs");
        if (!File.Exists(scriptFileName))
        {
            ShowCustomToast([Notification.Windows.Resources.App_OnStartup_NoScriptFile]);
            return;
        }
        
        string content;
        try
        {
            content = File.ReadAllText(scriptFileName);
        }
        catch (Exception exception)
        {
            ShowCustomToast([string.Format(Notification.Windows.Resources.App_OnStartup_ErrorReadingScript, exception.Message)]);
            return;
        }
        
        Toast.Instance.IsEmptyChanged += OnIsEmptyChanged;
        
        Toast.Instance.Show(message,
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

    void ShowNugetPackageVersionAvailableToast(string[] messageArguments)
    {
        var message = TryGet(messageArguments, 0, string.Empty);
        var packageName = TryGet(messageArguments, 1, string.Empty);
        var currentVersion = TryGet(messageArguments, 2, string.Empty);
        var newVersion = TryGet(messageArguments, 3, string.Empty);
        
        if (string.IsNullOrEmpty(message))
            message = string.Format(Notification.Windows.Resources.App_OnStartup_NugetPackageVersionAvailable, packageName, currentVersion, newVersion);
        
        Toast.Instance.IsEmptyChanged += OnIsEmptyChanged;
        
        Toast.Instance.Show(message);
    }

    void ShowCustomToast(string[] messageArguments)
    {
        var message = TryGet(messageArguments, 0, string.Empty);
        if (string.IsNullOrEmpty(message))
            message = Notification.Windows.Resources.App_OnStartup_CustomNotificationNoMessage;
        
        Toast.Instance.IsEmptyChanged += OnIsEmptyChanged;
        
        Toast.Instance.Show(message);
    }

    static string TryGet(string[] messageArguments, int index, string defaultValue)
    {
        return messageArguments.Length > index ? messageArguments[index] : defaultValue;
    }

    void OnInterConnectMessageReceived(InterConnectMessage message) => ShowToast(message);

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
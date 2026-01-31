using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.About;

[ViewModelFor<AboutView>]
public class AboutViewModel : OverlayModel
{
    public AboutViewModel()
    {
        CloseCommand = new RelayCommand(Close);
        OpenLogsCommand = new RelayCommand(OpenLogs);
        OpenDataCommand = new RelayCommand(OpenData);
        OpenGitHubCommand = new RelayCommand(OpenGitHub);
    }

    public string Version { get; } = GetVersion();

    public ICommand CloseCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand OpenDataCommand { get; }
    public ICommand OpenGitHubCommand { get; }

    static string GetVersion()
    {
        var versionFilePath = Path.Combine(AppContext.BaseDirectory, "PackageVersion.txt");
        if (File.Exists(versionFilePath))
            return File.ReadAllText(versionFilePath).Trim();

        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }

    void Close() => Dismiss();

    void OpenLogs()
    {
        var logsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MDK2", "Hub", "Logs");
        if (!Directory.Exists(logsPath))
            Directory.CreateDirectory(logsPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = logsPath,
            UseShellExecute = true
        });
    }

    void OpenData()
    {
        var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2", "Hub");
        if (!Directory.Exists(dataPath))
            Directory.CreateDirectory(dataPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = dataPath,
            UseShellExecute = true
        });
    }

    void OpenGitHub() =>
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/malforge/mdk2",
            UseShellExecute = true
        });
}
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.About;

/// <summary>
/// View model for the About dialog that shows version and links to logs/data/GitHub.
/// </summary>
[ViewModelFor<AboutView>]
public class AboutViewModel : OverlayModel
{
    readonly IFileStorageService _fileStorage;

    /// <summary>
    /// Initializes a new instance of the <see cref="AboutViewModel"/> class.
    /// </summary>
    public AboutViewModel(IFileStorageService fileStorage)
    {
        _fileStorage = fileStorage;
        CloseCommand = new RelayCommand(Close);
        OpenLogsCommand = new RelayCommand(OpenLogs);
        OpenDataCommand = new RelayCommand(OpenData);
        OpenGitHubCommand = new RelayCommand(OpenGitHub);
    }

    /// <summary>
    /// Gets the version string for the application.
    /// </summary>
    public string Version { get; } = GetVersion();

    /// <summary>
    /// Gets the command to close the About dialog.
    /// </summary>
    public ICommand CloseCommand { get; }
    /// <summary>
    /// Gets the command to open the logs folder.
    /// </summary>
    public ICommand OpenLogsCommand { get; }
    /// <summary>
    /// Gets the command to open the application data folder.
    /// </summary>
    public ICommand OpenDataCommand { get; }
    /// <summary>
    /// Gets the command to open the GitHub repository in a browser.
    /// </summary>
    public ICommand OpenGitHubCommand { get; }

    static string GetVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        
        if (version != null)
        {
            // Strip git metadata (everything after +)
            var plusIndex = version.IndexOf('+');
            if (plusIndex >= 0)
                version = version.Substring(0, plusIndex);
        }
        
        return version ?? assembly.GetName().Version?.ToString() ?? "Unknown";
    }

    void Close() => Dismiss();

    void OpenLogs()
    {
        var logsPath = Path.Combine(_fileStorage.GetLocalApplicationDataPath(), "Hub", "Logs");
        if (!_fileStorage.DirectoryExists(logsPath))
            _fileStorage.CreateDirectory(logsPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = logsPath,
            UseShellExecute = true
        });
    }

    void OpenData()
    {
        var dataPath = Path.Combine(_fileStorage.GetApplicationDataPath(), "Hub");
        if (!_fileStorage.DirectoryExists(dataPath))
            _fileStorage.CreateDirectory(dataPath);

        Process.Start(new ProcessStartInfo
        {
            FileName = dataPath,
            UseShellExecute = true
        });
    }

    void OpenGitHub() =>
        Process.Start(new ProcessStartInfo
        {
            FileName = EnvironmentMetadata.GitHubRepoUrl,
            UseShellExecute = true
        });
}

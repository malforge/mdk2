using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Actions;

public class AddExistingProjectAction : ActionItem
{
    readonly ICommonDialogs _dialogs;
    readonly IProjectService _projectService;
    readonly IShell _shell;

    public AddExistingProjectAction(IShell shell, ICommonDialogs dialogs, IProjectService projectService)
    {
        _shell = shell;
        _dialogs = dialogs;
        _projectService = projectService;
        AddCommand = new AsyncRelayCommand(AddExistingProjectAsync);
    }

    public ICommand AddCommand { get; }

    public override string? Category => null; // Global actions, no category

    public override bool ShouldShow(ProjectModel? selectedProject, bool canMakeScript, bool canMakeMod) =>
        // Always show
        true;

    async Task AddExistingProjectAsync()
    {
        // Get the main window from the application
        var window = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?
            .Windows.FirstOrDefault(w => w is ShellWindow);

        if (window == null)
            return;

        var filePickerOptions = new FilePickerOpenOptions
        {
            Title = "Select MDK² Project",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("C# Project Files")
                {
                    Patterns = new[] { "*.csproj" }
                }
            }
        };

        var files = await window.StorageProvider.OpenFilePickerAsync(filePickerOptions);
        if (files.Count == 0)
            return;

        var projectPath = files[0].Path.LocalPath;

        if (_projectService.TryAddProject(new CanonicalPath(projectPath), out var errorMessage))
        {
            // Success - the project overview will automatically refresh when it gets focus
            // TODO: Add event system to notify project list of changes
        }
        else
        {
            await _dialogs.ShowAsync(new ConfirmationMessage
            {
                Title = "Invalid Project",
                Message = errorMessage ?? "The selected file is not a valid MDK² project.",
                OkText = "OK",
                CancelText = "Cancel"
            });
        }
    }
}
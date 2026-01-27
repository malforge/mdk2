using System;
using System.Diagnostics;
using System.Windows.Input;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Action to open API documentation for the selected project.
/// </summary>
public class ApiDocsAction : ActionItem
{
    readonly ILogger _logger;
    readonly ProjectModel _project;
    readonly IProjectService _projectService;
    string? _description;
    ICommand? _executeCommand;
    string? _icon;
    string? _title;

    public ApiDocsAction(ProjectModel project, IProjectService projectService, ILogger logger)
    {
        _project = project;
        _projectService = projectService;
        _logger = logger;

        Title = "View API Documentation";
        Description = project.Type == ProjectType.IngameScript
            ? "Open the API documentation for scripts"
            : "Open the API documentation for mods";
        Icon = "fa-solid fa-book";

        ExecuteCommand = new RelayCommand(Execute);
    }

    public override string? Category => "Project";

    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    public ICommand? ExecuteCommand
    {
        get => _executeCommand;
        set => SetProperty(ref _executeCommand, value);
    }

    public override bool ShouldShow(ProjectModel? project, bool canMakeScript, bool canMakeMod) =>
        // Always show when a project is selected
        project != null;

    void Execute()
    {
        // TODO: Replace with actual URLs
        var url = _project.Type == ProjectType.Mod
            ? "https://malforge.github.io/spaceengineers/modapi/"
            : "https://malforge.github.io/spaceengineers/pbapi/";

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to open API documentation: {ex.Message}", ex);
        }
    }
}
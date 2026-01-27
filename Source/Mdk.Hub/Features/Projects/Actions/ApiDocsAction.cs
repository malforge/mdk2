using System;
using System.Linq;
using System.Windows.Input;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
/// Action to open API documentation for the selected project.
/// </summary>
public class ApiDocsAction : ActionItem
{
    readonly ProjectModel _project;
    readonly IProjectService _projectService;
    readonly ILogger _logger;
    string? _title;
    string? _description;
    string? _icon;
    ICommand? _executeCommand;

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
        
        ExecuteCommand = new Framework.RelayCommand(Execute);
    }

    public override bool ShouldShow(ProjectModel? project, bool canMakeScript, bool canMakeMod)
    {
        // Always show when a project is selected
        return project != null;
    }

    void Execute()
    {
        // TODO: Replace with actual URLs
        string url = _project.Type == ProjectType.Mod
            ? "https://malforge.github.io/spaceengineers/modapi/"
            : "https://malforge.github.io/spaceengineers/pbapi/";

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

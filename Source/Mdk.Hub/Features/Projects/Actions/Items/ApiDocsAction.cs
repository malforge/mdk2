using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Action to open API documentation for the selected project.
/// </summary>
[Singleton]
[ViewModelFor<ApiDocsActionView>]
public class ApiDocsAction : ActionItem
{
    readonly ILogger _logger;
    readonly IProjectService _projectService;
    string? _description;
    ICommand? _executeCommand;
    string? _icon;
    string? _title;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ApiDocsAction"/> class.
    /// </summary>
    /// <param name="projectService">The service for managing projects.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public ApiDocsAction(IProjectService projectService, ILogger logger)
    {
        _projectService = projectService;
        _logger = logger;

        Title = "View API Documentation";
        Description = "Open the API documentation";
        Icon = "fa-solid fa-book";

        ExecuteCommand = new RelayCommand(Execute);

        // Update description when selection changes
        _projectService.StateChanged += OnProjectStateChanged;
        UpdateDescription();
    }

    /// <summary>
    ///     Gets the action category.
    /// </summary>
    public override string Category => "Project";

    /// <summary>
    ///     Gets or sets the action title.
    /// </summary>
    public string? Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    ///     Gets or sets the action description.
    /// </summary>
    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    /// <summary>
    ///     Gets or sets the action icon.
    /// </summary>
    public string? Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    /// <summary>
    ///     Gets or sets the command to execute when the action is invoked.
    /// </summary>
    public ICommand? ExecuteCommand
    {
        get => _executeCommand;
        set => SetProperty(ref _executeCommand, value);
    }

    void OnProjectStateChanged(object? sender, EventArgs e)
    {
        UpdateDescription();
        RaiseShouldShowChanged();
    }

    void UpdateDescription()
    {
        var selectedProject = _projectService.State.SelectedProject;
        if (selectedProject.IsEmpty())
        {
            Description = "Open the API documentation";
            return;
        }

        var projectInfo = _projectService.GetProjects()
            .FirstOrDefault(p => CanonicalPathComparer.Instance.Equals(p.ProjectPath, selectedProject));

        Description = projectInfo?.Type == ProjectType.ProgrammableBlock
            ? "Open the API documentation for scripts"
            : "Open the API documentation for mods";
    }

    /// <summary>
    ///     Determines whether this action should be shown in the UI.
    /// </summary>
    public override bool ShouldShow() =>
        // Show when a project is selected
        !_projectService.State.SelectedProject.IsEmpty();

    void Execute()
    {
        var selectedProject = _projectService.State.SelectedProject;
        if (selectedProject.IsEmpty())
            return;

        var projectInfo = _projectService.GetProjects()
            .FirstOrDefault(p => CanonicalPathComparer.Instance.Equals(p.ProjectPath, selectedProject));

        if (projectInfo == null)
            return;

        var url = projectInfo.Type == ProjectType.Mod
            ? EnvironmentMetadata.ModApiDocsUrl
            : EnvironmentMetadata.PbApiDocsUrl;

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

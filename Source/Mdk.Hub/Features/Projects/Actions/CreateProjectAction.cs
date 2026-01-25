using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

public class CreateProjectAction : ActionItem
{
    public CreateProjectAction(
        IReadOnlyList<ProjectType> availableTypes, 
        AddExistingProjectAction addExistingAction,
        IProjectService projectService,
        ProjectActionsViewModel viewModel)
    {
        AvailableTypes = availableTypes;
        AddExistingAction = addExistingAction;
        Options = availableTypes.Select(t => new CreateOption(t, projectService, viewModel)).ToList();
    }

    public IReadOnlyList<ProjectType> AvailableTypes { get; }
    public IReadOnlyList<CreateOption> Options { get; }
    public AddExistingProjectAction AddExistingAction { get; }

    public override string? Category => null; // Global actions, no category
    
    public override bool IsGlobal => true; // Shared instance across all contexts

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        // Always show if we can create anything
        return canMakeScript || canMakeMod;
    }
}

public class CreateOption
{
    readonly IProjectService _projectService;
    readonly ProjectActionsViewModel _viewModel;
    
    public CreateOption(ProjectType projectType, IProjectService projectService, ProjectActionsViewModel viewModel)
    {
        ProjectType = projectType;
        _projectService = projectService;
        _viewModel = viewModel;
        
        Title = projectType == ProjectType.IngameScript 
            ? "New Programmable Block Script" 
            : "New Mod";
        Description = projectType == ProjectType.IngameScript
            ? "Create a new Programmable Block Script project"
            : "Create a new Space Engineers mod project";

        _createCommand = new AsyncRelayCommand(CreateProjectAsync);
    }
    
    public ProjectType ProjectType { get; }
    public string Title { get; }
    public string Description { get; }

    readonly AsyncRelayCommand _createCommand;
    public ICommand CreateCommand => _createCommand;

    async Task CreateProjectAsync()
    {
        var defaultLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        
        // Try to get the last used location for this project type
        var settingsKey = ProjectType == ProjectType.IngameScript 
            ? "NewProject.LastLocation.IngameScript" 
            : "NewProject.LastLocation.Mod";
        var lastLocation = _projectService.Settings.GetValue(settingsKey, defaultLocation);
        
        var result = await _viewModel.ShowNewProjectDialogAsync(new NewProjectDialog.NewProjectDialogMessage
        {
            Title = Title,
            Message = Description,
            ProjectType = ProjectType,
            DefaultLocation = lastLocation,
            OkText = "Create",
            CancelText = "Cancel"
        });

        if (result == null)
            return; // User cancelled
        
        // Save the location for next time
        _projectService.Settings.SetValue(settingsKey, result.Value.Location);

        // Show busy indicator
        var busyOverlay = new CommonDialogs.BusyOverlayViewModel("Creating project...");
        var busyTask = _viewModel.ShowBusyOverlayAsync(busyOverlay);

        try
        {
            // Create the project
            var createResult = ProjectType == ProjectType.IngameScript
                ? await _projectService.CreateProgrammableBlockProjectAsync(result.Value.ProjectName, result.Value.Location)
                : await _projectService.CreateModProjectAsync(result.Value.ProjectName, result.Value.Location);
            
            var projectPath = createResult.ProjectPath;
            var errorMessage = createResult.ErrorMessage;

            if (projectPath == null)
            {
                // Dismiss busy indicator
                busyOverlay.Dismiss();
                await busyTask;
                
                // Show error
                await _viewModel.ShowErrorAsync("Project Creation Failed", errorMessage ?? "Unknown error occurred");
                return;
            }

            // Add project to registry
            string? addError;
            if (!_projectService.TryAddProject(projectPath.Value, out addError))
            {
                // Dismiss busy indicator
                busyOverlay.Dismiss();
                await busyTask;
                
                await _viewModel.ShowErrorAsync("Failed to Add Project", addError ?? "Unknown error occurred");
                return;
            }

            // Dismiss busy indicator
            busyOverlay.Dismiss();
            await busyTask;

            // Navigate to the new project (selects it)
            _projectService.NavigateToProject(projectPath.Value);

            // Open the options drawer
            _viewModel.OpenOptionsDrawer();
        }
        catch (Exception ex)
        {
            // Dismiss busy indicator on exception
            busyOverlay.Dismiss();
            await busyTask;
            
            await _viewModel.ShowErrorAsync("Unexpected Error", $"An error occurred while creating the project: {ex.Message}");
        }
    }
}

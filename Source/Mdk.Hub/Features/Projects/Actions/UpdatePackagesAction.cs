using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

public class UpdatePackagesAction : ActionItem, IDisposable
{
    readonly AsyncRelayCommand _updateThisProjectCommand;
    readonly AsyncRelayCommand _updateAllProjectsCommand;
    readonly IShell _shell;
    readonly IProjectService _projectService;
    ProjectModel? _project;

    public UpdatePackagesAction(ProjectModel project, IShell shell, IProjectService projectService)
    {
        _project = project;
        _shell = shell;
        _projectService = projectService;
        _project.PropertyChanged += OnProjectPropertyChanged;
        _updateThisProjectCommand = new AsyncRelayCommand(UpdateThisProjectAsync);
        _updateAllProjectsCommand = new AsyncRelayCommand(UpdateAllProjectsAsync);
    }

    public override string Category => "Updates";

    public override bool ShouldShow(ProjectListItem? selectedProject, bool canMakeScript, bool canMakeMod)
    {
        // Only show if a project is selected and it needs updates
        return selectedProject is ProjectModel model && model.NeedsUpdate;
    }

    public ICommand UpdateThisProjectCommand => _updateThisProjectCommand;
    public ICommand UpdateAllProjectsCommand => _updateAllProjectsCommand;

    void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectModel.NeedsUpdate))
        {
            RaiseShouldShowChanged();
        }
    }
    
    public void Dispose()
    {
        if (_project != null)
        {
            _project.PropertyChanged -= OnProjectPropertyChanged;
            _project = null;
        }
    }

    async Task UpdateThisProjectAsync()
    {
        if (_project == null)
            return;

        var busyOverlay = new BusyOverlayViewModel("Updating MDK packages...");
        _shell.AddOverlay(busyOverlay);

        try
        {
            // Update Malware.MDK package for this specific project
            var (updateSuccess, updateError) = await RunDotNetCommandAsync("add", _project.ProjectPath.Value!, "package", "Malware.MDK");
            
            if (!updateSuccess)
            {
                await ShowErrorWithDetailsAsync("Update Failed", "Failed to update MDK package.", updateError);
                return;
            }

            // Run dotnet restore on this specific project
            var (restoreSuccess, restoreError) = await RunDotNetCommandAsync("restore", _project.ProjectPath.Value!);
            
            if (!restoreSuccess)
            {
                await ShowErrorWithDetailsAsync("Restore Failed", "Package was updated but restore failed. The project may not build correctly.", restoreError);
                return;
            }

            // Reset update state
            _project.NeedsUpdate = false;
            _project.UpdateCount = 0;
            _projectService.ClearProjectUpdateState(_project.ProjectPath);
        }
        finally
        {
            busyOverlay.Dismiss();
        }
    }

    async Task UpdateAllProjectsAsync()
    {
        // Get all projects with updates from the service
        var projectsToUpdate = _projectService.GetProjects()
            .Where(p => p.NeedsUpdate)
            .ToList();

        if (projectsToUpdate.Count == 0)
            return;

        var busyOverlay = new BusyOverlayViewModel($"Updating 0 of {projectsToUpdate.Count} project(s)...")
        {
            IsIndeterminate = false,
            Progress = 0
        };
        _shell.AddOverlay(busyOverlay);

        var failures = new List<(string projectName, string error)>();
        var completedCount = 0;

        try
        {
            // Update all projects in parallel
            await Task.WhenAll(projectsToUpdate.Select(async projectInfo =>
            {
                try
                {
                    // Update package
                    var (updateSuccess, updateError) = await RunDotNetCommandAsync("add", projectInfo.ProjectPath.Value!, "package", "Malware.MDK");
                    if (!updateSuccess)
                    {
                        lock (failures)
                        {
                            failures.Add((projectInfo.Name, $"Update failed: {updateError}"));
                            completedCount++;
                            UpdateProgress(busyOverlay, completedCount, projectsToUpdate.Count);
                        }
                        return;
                    }

                    // Restore
                    var (restoreSuccess, restoreError) = await RunDotNetCommandAsync("restore", projectInfo.ProjectPath.Value!);
                    if (!restoreSuccess)
                    {
                        lock (failures)
                        {
                            failures.Add((projectInfo.Name, $"Restore failed: {restoreError}"));
                            completedCount++;
                            UpdateProgress(busyOverlay, completedCount, projectsToUpdate.Count);
                        }
                        return;
                    }

                    // Success - clear update state in service (will trigger event to update view models)
                    _projectService.ClearProjectUpdateState(projectInfo.ProjectPath);
                    
                    lock (failures)
                    {
                        completedCount++;
                        UpdateProgress(busyOverlay, completedCount, projectsToUpdate.Count);
                    }
                }
                catch (Exception ex)
                {
                    lock (failures)
                    {
                        failures.Add((projectInfo.Name, $"Unexpected error: {ex.Message}"));
                        completedCount++;
                        UpdateProgress(busyOverlay, completedCount, projectsToUpdate.Count);
                    }
                }
            }));

            // Show results
            var successCount = completedCount - failures.Count;
            if (failures.Count == 0)
            {
                _shell.ShowToast($"Successfully updated {successCount} project(s)");
            }
            else if (successCount > 0)
            {
                // Partial success
                var errorSummary = string.Join(Environment.NewLine + Environment.NewLine, 
                    failures.Select(f => $"{f.projectName}:{Environment.NewLine}{f.error}"));
                await ShowErrorWithDetailsAsync(
                    "Partial Update Success", 
                    $"Updated {successCount} project(s), but {failures.Count} failed:", 
                    errorSummary);
            }
            else
            {
                // Total failure
                var errorSummary = string.Join(Environment.NewLine + Environment.NewLine, 
                    failures.Select(f => $"{f.projectName}:{Environment.NewLine}{f.error}"));
                await ShowErrorWithDetailsAsync(
                    "Update Failed", 
                    $"Failed to update all {failures.Count} project(s):", 
                    errorSummary);
            }
        }
        finally
        {
            busyOverlay.Dismiss();
        }
    }

    void UpdateProgress(BusyOverlayViewModel overlay, int completed, int total)
    {
        overlay.Progress = (double)completed / total * 100;
        overlay.Message = $"Updating {completed} of {total} project(s)...";
    }

    async Task<(bool success, string error)> RunDotNetCommandAsync(params string[] args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        using var process = Process.Start(startInfo);
        if (process == null)
            return (false, "Failed to start dotnet process");

        var errorOutput = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0)
        {
            var output = await process.StandardOutput.ReadToEndAsync();
            var fullError = string.IsNullOrWhiteSpace(errorOutput) ? output : errorOutput;
            
            // "no versions available" means package doesn't exist or is already latest - not really an error
            if (fullError.Contains("no versions available", StringComparison.OrdinalIgnoreCase))
                return (true, string.Empty);
            
            return (false, fullError);
        }

        return (true, string.Empty);
    }

    async Task ShowErrorAsync(string title, string message)
    {
        var model = new ErrorDetailsViewModel
        {
            Title = title,
            Message = message,
            Details = string.Empty
        };

        await _shell.ShowOverlayAsync(model);
    }

    async Task ShowErrorWithDetailsAsync(string title, string message, string details)
    {
        var model = new ErrorDetailsViewModel
        {
            Title = title,
            Message = message,
            Details = details
        };

        await _shell.ShowOverlayAsync(model);
    }
}

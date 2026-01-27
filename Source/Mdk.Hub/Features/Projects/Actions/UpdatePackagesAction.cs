using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions;

public class UpdatePackagesAction : ActionItem, IDisposable
{
    readonly IProjectService _projectService;
    readonly IShell _shell;
    readonly AsyncRelayCommand _updateAllProjectsCommand;
    readonly AsyncRelayCommand _updateThisProjectCommand;
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

    public ICommand UpdateThisProjectCommand => _updateThisProjectCommand;
    public ICommand UpdateAllProjectsCommand => _updateAllProjectsCommand;

    public void Dispose()
    {
        if (_project != null)
        {
            _project.PropertyChanged -= OnProjectPropertyChanged;
            _project = null;
        }
    }

    public override bool ShouldShow(ProjectModel? selectedProject, bool canMakeScript, bool canMakeMod) =>
        // Only show if a project is selected and it needs updates
        selectedProject is ProjectModel model && model.NeedsUpdate;

    void OnProjectPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectModel.NeedsUpdate))
            RaiseShouldShowChanged();
    }

    async Task UpdateThisProjectAsync()
    {
        if (_project == null)
            return;

        var busyOverlay = new BusyOverlayViewModel("Checking for updates...");
        _shell.AddOverlay(busyOverlay);

        try
        {
            // Try to use cached updates first (from background checker)
            var updates = _projectService.GetCachedUpdates(_project.ProjectPath);

            // If not cached, query NuGet
            if (updates == null)
                updates = await _projectService.CheckForPackageUpdatesAsync(_project.ProjectPath);

            if (updates.Count == 0)
            {
                busyOverlay.Message = "No updates available";
                await Task.Delay(1500);
                return;
            }

            busyOverlay.Message = $"Updating {updates.Count} package(s)...";

            // Update packages
            var success = await _projectService.UpdatePackagesAsync(_project.ProjectPath, updates);

            if (success)
            {
                busyOverlay.Message = "Packages updated successfully!";
                await Task.Delay(1500);
            }
            else
                await ShowErrorAsync("Update Failed", "Failed to update packages. Check the log for details.");
        }
        finally
        {
            busyOverlay.Dismiss();
        }
    }

    async Task UpdateAllProjectsAsync()
    {
        // Get all projects with updates
        var projectsToUpdate = _projectService.GetProjects()
            .Where(p => p.NeedsUpdate)
            .ToList();

        if (projectsToUpdate.Count == 0)
            return;

        var busyOverlay = new BusyOverlayViewModel($"Checking {projectsToUpdate.Count} project(s) for updates...")
        {
            IsIndeterminate = false,
            Progress = 0
        };
        busyOverlay.EnableCancellation();
        _shell.AddOverlay(busyOverlay);

        var failures = new List<(string projectName, string error)>();
        var wasCancelled = false;

        try
        {
            // Phase 1: Get update info (use cache when available, otherwise query NuGet)
            var updateTasks = projectsToUpdate.Select(async projectInfo =>
            {
                try
                {
                    // Try to use cached updates first (from background checker)
                    var updates = _projectService.GetCachedUpdates(projectInfo.ProjectPath);

                    // If not cached, query NuGet
                    if (updates == null)
                        updates = await _projectService.CheckForPackageUpdatesAsync(projectInfo.ProjectPath, busyOverlay.CancellationToken);

                    return (projectInfo, updates, error: null);
                }
                catch (OperationCanceledException)
                {
                    throw; // Propagate cancellation
                }
                catch (Exception ex)
                {
                    return (projectInfo, updates: (IReadOnlyList<PackageUpdateInfo>)new List<PackageUpdateInfo>(), error: ex.Message);
                }
            }).ToList();

            // Wait for all checks to complete (with progress updates)
            var completedChecks = 0;
            while (completedChecks < updateTasks.Count)
            {
                if (busyOverlay.CancellationToken.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                await Task.Delay(100); // Poll for completion
                var nowCompleted = updateTasks.Count(t => t.IsCompleted);
                if (nowCompleted > completedChecks)
                {
                    completedChecks = nowCompleted;
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        busyOverlay.Progress = (double)completedChecks / updateTasks.Count * 0.5; // First 50% is checking
                        busyOverlay.Message = $"Checking for updates... ({completedChecks}/{updateTasks.Count})";
                    });
                }
            }

            if (wasCancelled)
            {
                await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Message = "Cancelled"; });
                await Task.Delay(1000);
                return;
            }

            var checkResults = await Task.WhenAll(updateTasks);

            // Collect any check failures
            foreach (var (projectInfo, _, error) in checkResults.Where(r => r.error != null))
                failures.Add((projectInfo.Name, error!));

            // Phase 2: Update projects sequentially (to avoid overwhelming the system)
            var projectsWithUpdates = checkResults
                .Where(r => r.error == null && r.updates.Count > 0)
                .ToList();

            if (projectsWithUpdates.Count == 0)
            {
                await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Message = failures.Count > 0 ? "Check failed" : "No updates available"; });
                await Task.Delay(1500);

                if (failures.Count == 0)
                    _shell.ShowToast("All projects are up to date");

                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Message = $"Updating {projectsWithUpdates.Count} project(s)..."; });

            var completedCount = 0;
            foreach (var (projectInfo, updates, _) in projectsWithUpdates)
            {
                // Check for cancellation between updates
                if (busyOverlay.CancellationToken.IsCancellationRequested)
                {
                    wasCancelled = true;
                    break;
                }

                try
                {
                    await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Message = $"Updating {projectInfo.Name}... ({completedCount + 1}/{projectsWithUpdates.Count})"; });

                    var success = await _projectService.UpdatePackagesAsync(projectInfo.ProjectPath, updates, busyOverlay.CancellationToken);

                    if (!success)
                        failures.Add((projectInfo.Name, "Failed to update packages"));
                }
                catch (OperationCanceledException)
                {
                    wasCancelled = true;
                    break;
                }
                catch (Exception ex)
                {
                    failures.Add((projectInfo.Name, ex.Message));
                }
                finally
                {
                    completedCount++;
                    var progress = 0.5 + (double)completedCount / projectsWithUpdates.Count * 0.5; // Second 50% is updating
                    await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Progress = progress; });
                }
            }

            // Show results
            if (wasCancelled)
            {
                await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Message = $"Cancelled after updating {completedCount}/{projectsWithUpdates.Count} project(s)"; });
                await Task.Delay(1500);
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() => { busyOverlay.Message = "Update complete!"; });
                await Task.Delay(1000);
            }

            if (failures.Count == 0 && !wasCancelled)
                _shell.ShowToast($"Successfully updated {completedCount} project(s)");
            else if (failures.Count > 0)
            {
                // Show errors only if there were actual failures
                if (completedCount - failures.Count > 0)
                {
                    // Partial success
                    var errorSummary = string.Join(Environment.NewLine, failures.Select(f => $"- {f.projectName}: {f.error}"));
                    await ShowErrorWithDetailsAsync(
                        "Partial Update Success",
                        $"Updated {completedCount - failures.Count} project(s), but {failures.Count} failed:",
                        errorSummary);
                }
                else
                {
                    // Total failure
                    var errorSummary = string.Join(Environment.NewLine, failures.Select(f => $"- {f.projectName}: {f.error}"));
                    await ShowErrorWithDetailsAsync(
                        "Update Failed",
                        $"Failed to update all {failures.Count} project(s):",
                        errorSummary);
                }
            }
            // If wasCancelled && failures.Count == 0, we already showed the cancelled message above
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
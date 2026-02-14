using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Utility;
using NuGet.Versioning;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Handles background checking of projects for available package updates.
/// </summary>
class ProjectUpdateChecker
{
    readonly Lock _lock = new();
    readonly ILogger _logger;
    readonly IProjectService _projectService;
    readonly Queue<CanonicalPath> _queue = new();
    readonly HashSet<CanonicalPath> _queued = new();
    readonly IProjectRegistry _registry;
    CancellationTokenSource? _cancellationTokenSource;
    bool _hasVersionData;
    Dictionary<string, string>? _latestVersions;
    Task? _processingTask;

    public ProjectUpdateChecker(ILogger logger, IProjectService projectService, IUpdateManager updateManager, IProjectRegistry registry, IShell shell)
    {
        _logger = logger;
        _projectService = projectService;
        _registry = registry;
        // When version data is available, start checking projects
        updateManager.WhenVersionCheckUpdates(OnVersionDataUpdated);
        // Re-check projects when user requests refresh (Ctrl+R)
        shell.RefreshRequested += OnRefreshRequested;
    }

    public event EventHandler<ProjectUpdateAvailableEventArgs>? ProjectUpdateAvailable;

    void OnVersionDataUpdated(VersionCheckCompletedEventArgs versionData)
    {
        _logger.Info($"Version data available, storing latest versions for {versionData.Packages.Count} MDK package(s)");
        _hasVersionData = true;

        // Store the latest versions from the version check
        _latestVersions = new Dictionary<string, string>();
        foreach (var package in versionData.Packages)
            _latestVersions[package.PackageId] = package.LatestVersion;

        // Start processing if we have queued projects
        lock (_lock)
        {
            if (_queue.Count > 0 && _processingTask == null)
                StartProcessing();
        }

        var projects = _registry.GetProjects();
        QueueProjectsCheck(projects.Select(p => p.ProjectPath));
    }

    void OnRefreshRequested(object? sender, EventArgs e)
    {
        if (!_hasVersionData)
        {
            _logger.Debug("Refresh requested but no version data yet, skipping project update check");
            return;
        }

        lock (_lock)
        {
            // If already processing, don't queue more work
            if (_processingTask != null)
            {
                _logger.Debug("Project update check already in progress, skipping refresh");
                return;
            }
        }

        _logger.Info("Refresh requested, re-checking all projects for package updates");
        var projects = _registry.GetProjects();
        QueueProjectsCheck(projects.Select(p => p.ProjectPath));
    }

    /// <summary>
    ///     Queues a project for update checking, optionally with priority to check it next.
    /// </summary>
    /// <param name="projectPath">The path to the project.</param>
    /// <param name="priority">If true, the project will be checked next (moved to front of queue).</param>
    public void QueueProjectCheck(CanonicalPath projectPath, bool priority = false)
    {
        lock (_lock)
        {
            // If already queued and not priority, skip
            if (_queued.Contains(projectPath) && !priority)
                return;

            // Remove from queue if already there (we'll re-add it)
            if (_queued.Contains(projectPath))
            {
                var tempQueue = new Queue<CanonicalPath>();
                while (_queue.Count > 0)
                {
                    var item = _queue.Dequeue();
                    if (item != projectPath)
                        tempQueue.Enqueue(item);
                }
                _queue.Clear();
                foreach (var item in tempQueue)
                    _queue.Enqueue(item);
            }

            // Add to front or back depending on priority
            if (priority)
            {
                // Add to front by creating new queue
                var tempQueue = new Queue<CanonicalPath>();
                tempQueue.Enqueue(projectPath);
                while (_queue.Count > 0)
                    tempQueue.Enqueue(_queue.Dequeue());
                _queue.Clear();
                foreach (var item in tempQueue)
                    _queue.Enqueue(item);
            }
            else
                _queue.Enqueue(projectPath);

            _queued.Add(projectPath);

            // Start processing if we have version data and not already running
            if (_hasVersionData && _processingTask == null)
                StartProcessing();
        }
    }

    void QueueProjectsCheck(IEnumerable<CanonicalPath> projectPaths)
    {
        foreach (var path in projectPaths)
            QueueProjectCheck(path);
    }

    /// <summary>
    ///     Removes a project from the queue if it's waiting.
    /// </summary>
    public void RemoveFromQueue(CanonicalPath projectPath)
    {
        lock (_lock)
        {
            if (!_queued.Contains(projectPath))
                return;

            _queued.Remove(projectPath);

            var tempQueue = new Queue<CanonicalPath>();
            while (_queue.Count > 0)
            {
                var item = _queue.Dequeue();
                if (item != projectPath)
                    tempQueue.Enqueue(item);
            }
            _queue.Clear();
            foreach (var item in tempQueue)
                _queue.Enqueue(item);
        }
    }

    void StartProcessing()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _processingTask = Task.Run(async () => await ProcessQueueAsync(_cancellationTokenSource.Token));
    }

    async Task ProcessQueueAsync(CancellationToken cancellationToken)
    {
        _logger.Info("Starting background project update check processing");

        while (!cancellationToken.IsCancellationRequested)
        {
            CanonicalPath projectPath;

            lock (_lock)
            {
                if (_queue.Count == 0)
                {
                    _processingTask = null;
                    _logger.Info("Project update check queue empty, stopping processing");
                    return;
                }

                projectPath = _queue.Dequeue();
                _queued.Remove(projectPath);
            }

            try
            {
                // Small delay to avoid hammering the system
                await Task.Delay(100, cancellationToken);

                // Check if project still exists
                if (!File.Exists(projectPath.Value))
                {
                    _logger.Debug($"Skipping update check for deleted project: {projectPath}");
                    continue;
                }

                _logger.Debug($"Checking project for updates: {projectPath}");

                // Check for actual package updates using pre-fetched latest versions
                var updates = CheckProjectForUpdates(projectPath);

                if (updates.Count > 0)
                {
                    _logger.Info($"Updates available for {projectPath}: {updates.Count} package(s)");
                    ProjectUpdateAvailable?.Invoke(this,
                        new ProjectUpdateAvailableEventArgs
                        {
                            ProjectPath = projectPath,
                            AvailableUpdates = updates
                        });
                }
                else
                {
                    _logger.Debug($"No updates available for {projectPath}");
                    ProjectUpdateAvailable?.Invoke(this,
                        new ProjectUpdateAvailableEventArgs
                        {
                            ProjectPath = projectPath,
                            AvailableUpdates = Array.Empty<PackageUpdateInfo>()
                        });
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error checking project {projectPath} for updates: {ex.Message}");
            }
        }

        _logger.Info("Project update check processing cancelled");
    }

    List<PackageUpdateInfo> CheckProjectForUpdates(CanonicalPath projectPath)
    {
        var updates = new List<PackageUpdateInfo>();

        if (_latestVersions == null)
            return updates;

        try
        {
            // Get current package versions from .csproj
            var currentVersions = _projectService.GetMdkPackageVersions(projectPath);

            // Compare against pre-fetched latest versions
            foreach (var (packageId, currentVersion) in currentVersions)
            {
                if (_latestVersions.TryGetValue(packageId, out var latestVersion))
                {
                    // Use semantic version comparison
                    if (NuGetVersion.TryParse(currentVersion, out var currentVer) && NuGetVersion.TryParse(latestVersion, out var latestVer) && latestVer > currentVer)
                    {
                        updates.Add(new PackageUpdateInfo
                        {
                            PackageId = packageId,
                            CurrentVersion = currentVersion,
                            LatestVersion = latestVersion
                        });
                        _logger.Debug($"Update available for {packageId}: {currentVersion} -> {latestVersion}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Error checking {projectPath} for updates", ex);
        }

        return updates;
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _processingTask?.Wait(TimeSpan.FromSeconds(1));
        _cancellationTokenSource?.Dispose();
    }
}
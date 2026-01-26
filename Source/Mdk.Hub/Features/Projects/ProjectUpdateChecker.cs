using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Updates;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
/// Handles background checking of projects for available package updates.
/// </summary>
class ProjectUpdateChecker
{
    readonly ILogger _logger;
    readonly IProjectService _projectService;
    readonly Queue<CanonicalPath> _queue = new();
    readonly HashSet<CanonicalPath> _queued = new();
    readonly object _lock = new();
    CancellationTokenSource? _cancellationTokenSource;
    Task? _processingTask;
    bool _hasVersionData;
    Dictionary<string, string>? _latestVersions;

    public event EventHandler<ProjectUpdateAvailableEventArgs>? ProjectUpdateAvailable;

    public ProjectUpdateChecker(ILogger logger, IProjectService projectService)
    {
        _logger = logger;
        _projectService = projectService;
    }

    /// <summary>
    /// Starts background processing when version data becomes available.
    /// </summary>
    public void OnVersionDataAvailable(VersionCheckCompletedEventArgs versionData)
    {
        _logger.Info($"Version data available, storing latest versions for {versionData.Packages.Count} MDK package(s)");
        _hasVersionData = true;
        
        // Store the latest versions from the version check
        _latestVersions = new Dictionary<string, string>();
        foreach (var package in versionData.Packages)
        {
            _latestVersions[package.PackageId] = package.LatestVersion;
        }
        
        // Start processing if we have queued projects
        lock (_lock)
        {
            if (_queue.Count > 0 && _processingTask == null)
            {
                StartProcessing();
            }
        }
    }

    /// <summary>
    /// Queues a project for update checking. If project is selected, moves to front of queue.
    /// </summary>
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
            {
                _queue.Enqueue(projectPath);
            }

            _queued.Add(projectPath);

            // Start processing if we have version data and not already running
            if (_hasVersionData && _processingTask == null)
            {
                StartProcessing();
            }
        }
    }

    /// <summary>
    /// Queues multiple projects for checking.
    /// </summary>
    public void QueueProjectsCheck(IEnumerable<CanonicalPath> projectPaths)
    {
        foreach (var path in projectPaths)
        {
            QueueProjectCheck(path, priority: false);
        }
    }

    /// <summary>
    /// Removes a project from the queue if it's waiting.
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
                    ProjectUpdateAvailable?.Invoke(this, new ProjectUpdateAvailableEventArgs
                    {
                        ProjectPath = projectPath,
                        AvailableUpdates = updates
                    });
                }
                else
                {
                    _logger.Debug($"No updates available for {projectPath}");
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
                    if (latestVersion != currentVersion)
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

/// <summary>
/// Event arguments for when a project has updates available.
/// </summary>
public class ProjectUpdateAvailableEventArgs : EventArgs
{
    public required CanonicalPath ProjectPath { get; init; }
    public required IReadOnlyList<PackageUpdateInfo> AvailableUpdates { get; init; }
}

/// <summary>
/// Information about an available package update.
/// </summary>
public record PackageUpdateInfo
{
    public required string PackageId { get; init; }
    public required string CurrentVersion { get; init; }
    public required string LatestVersion { get; init; }
}

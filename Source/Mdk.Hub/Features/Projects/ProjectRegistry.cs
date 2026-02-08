using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects;

/// <summary>
///     Stores and manages the registry of known MDK projects.
///     Projects are persisted to %appdata%\MDK2\projects.json
/// </summary>
[Singleton<IProjectRegistry>]
public class ProjectRegistry : IProjectRegistry
{
    readonly ILogger _logger;
    readonly IFileStorageService _fileStorage;
    readonly string _registryPath;
    readonly string _versionFilesPath;
    List<ProjectInfo> _projects = new();

    /// <summary>
    ///     Initializes a new instance of the ProjectRegistry class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <param name="fileStorage">File storage service for filesystem operations.</param>
    public ProjectRegistry(ILogger logger, IFileStorageService fileStorage)
    {
        _logger = logger;
        _fileStorage = fileStorage;
        _registryPath = fileStorage.GetApplicationDataPath("projects.json");
        _versionFilesPath = fileStorage.GetApplicationDataPath();
        Load();
    }

    /// <summary>
    ///     Internal constructor for testing that allows custom registry path.
    /// </summary>
    internal ProjectRegistry(ILogger logger, IFileStorageService fileStorage, string registryPath, string versionFilesPath)
    {
        _logger = logger;
        _fileStorage = fileStorage;
        _registryPath = registryPath;
        _versionFilesPath = versionFilesPath;
        Load();
    }

    /// <inheritdoc />
    public IReadOnlyList<ProjectInfo> GetProjects()
    {
        // Update last referenced time based on file access
        RefreshLastReferencedTimes();
        return _projects.OrderByDescending(p => p.LastReferenced).ToList();
    }

    /// <inheritdoc />
    public void AddOrUpdateProject(ProjectInfo project)
    {
        var existing = _projects.FirstOrDefault(p => p.ProjectPath == project.ProjectPath);

        if (existing != null)
        {
            _logger.Debug($"Updating project: {project.Name}");
            _projects.Remove(existing);
        }
        else
            _logger.Info($"Adding new project: {project.Name} at {project.ProjectPath}");

        _projects.Add(project with { LastReferenced = DateTimeOffset.Now });
        Save();
    }

    /// <inheritdoc />
    public void RemoveProject(string projectPath)
    {
        var canonicalPath = new CanonicalPath(projectPath);
        var removed = _projects.RemoveAll(p => p.ProjectPath == canonicalPath);
        if (removed > 0)
        {
            _logger.Info($"Removed project: {projectPath}");
            Save();
        }
    }

    void Load()
    {
        if (_fileStorage.FileExists(_registryPath))
        {
            try
            {
                var json = _fileStorage.ReadAllText(_registryPath);
                _projects = JsonSerializer.Deserialize<List<ProjectInfo>>(json) ?? new List<ProjectInfo>();
                _logger.Info($"Loaded {_projects.Count} projects from registry");
            }
            catch (JsonException ex)
            {
                _logger.Error($"Failed to parse project registry file: {_registryPath}", ex);
                _projects = new List<ProjectInfo>();
            }
        }
        else
        {
            _logger.Info("No existing registry found, importing from .version files");
            _projects = new List<ProjectInfo>();
            ImportFromVersionFiles();
        }
    }

    void ImportFromVersionFiles()
    {
        if (!_fileStorage.DirectoryExists(_versionFilesPath))
            return;

        try
        {
            var versionFiles = _fileStorage.GetFiles(_versionFilesPath, "*.version");
            if (versionFiles.Length == 0)
                return;

            var importedCount = 0;

            foreach (var versionFile in versionFiles)
            {
                try
                {
                    var projectPath = ExtractProjectPathFromVersionFile(versionFile);
                    if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
                        continue;

                    // Validate it's a real MDK2 project
                    if (ProjectDetector.TryDetectProject(projectPath, out var projectInfo) && projectInfo != null)
                    {
                        // Check if not already in the list
                        if (!_projects.Any(p => p.ProjectPath == new CanonicalPath(projectPath)))
                        {
                            _projects.Add(projectInfo);
                            importedCount++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warning($"Failed to import version file: {versionFile} - {ex.Message}");
                }
            }

            if (importedCount > 0)
            {
                _logger.Info($"Imported {importedCount} projects from .version files");
                Save();
            }

            // Clean up legacy .version files now that import is complete
            DeleteVersionFiles(versionFiles);
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to import from .version files", ex);
        }
    }

    void DeleteVersionFiles(string[] versionFiles)
    {
        var deletedCount = 0;
        foreach (var versionFile in versionFiles)
        {
            try
            {
                _fileStorage.DeleteFile(versionFile);
                deletedCount++;
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to delete legacy version file: {versionFile} - {ex.Message}");
            }
        }

        if (deletedCount > 0)
            _logger.Info($"Cleaned up {deletedCount} legacy .version files");
    }

    string? ExtractProjectPathFromVersionFile(string versionFilePath)
    {
        try
        {
            var content = _fileStorage.ReadAllText(versionFilePath);
            if (!Ini.TryParse(content, out var ini))
                return null;

            return ini["mdk"]["projectpath"].ToString();
        }
        catch
        {
            return null;
        }
    }

    void Save()
    {
        const int maxRetries = 3;
        const int retryDelayMs = 100;

        for (var attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var directory = Path.GetDirectoryName(_registryPath);
                if (!string.IsNullOrEmpty(directory) && !_fileStorage.DirectoryExists(directory))
                    _fileStorage.CreateDirectory(directory);

                // Filter out simulated projects before saving
                var projectsToSave = _projects.Where(p => !p.Flags.HasFlag(ProjectFlags.Simulated)).ToList();
                var json = JsonSerializer.Serialize(projectsToSave, new JsonSerializerOptions { WriteIndented = true });

                // Use temp file + rename for atomic write
                var tempPath = _registryPath + ".tmp";
                _fileStorage.WriteAllText(tempPath, json);

                // Atomic replace
                if (_fileStorage.FileExists(_registryPath))
                    _fileStorage.DeleteFile(_registryPath);
                _fileStorage.MoveFile(tempPath, _registryPath);

                return; // Success
            }
            catch (IOException ex) when (attempt < maxRetries - 1)
            {
                _logger.Warning($"Registry save attempt {attempt + 1} failed (retrying): {ex.Message}");
                Thread.Sleep(retryDelayMs);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.Error("Registry save failed: permission denied", ex);
                return;
            }
            catch (Exception ex)
            {
                _logger.Error("Registry save failed", ex);
                return;
            }
        }

        _logger.Error($"Registry save failed after {maxRetries} attempts");
    }

    void RefreshLastReferencedTimes()
    {
        var needsSave = false;
        for (var i = 0; i < _projects.Count; i++)
        {
            var project = _projects[i];
            if (project.ProjectPath.Value != null && _fileStorage.FileExists(project.ProjectPath.Value))
            {
                var lastWrite = _fileStorage.GetLastWriteTimeUtc(project.ProjectPath.Value);
                if (lastWrite > project.LastReferenced.UtcDateTime)
                {
                    _projects[i] = project with { LastReferenced = new DateTimeOffset(lastWrite) };
                    needsSave = true;
                }
            }
        }

        if (needsSave)
            Save();
    }
}

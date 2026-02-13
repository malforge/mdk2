using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Overview;

/// <summary>
///     View model for a project displayed in the Hub UI.
/// </summary>
public class ProjectModel : ViewModel
{
    // Limit concurrent thumbnail loads to avoid overwhelming the system
    static readonly SemaphoreSlim _thumbnailLoadSemaphore = new(2, 2);
    readonly AsyncRelayCommand _deleteCommand;
    readonly IProjectService? _projectService;
    readonly AsyncRelayCommand _removeFromHubCommand;

    readonly IShell _shell;
    bool _hasUnsavedChanges;
    bool _isSelected;
    DateTimeOffset _lastReferenced;
    string _name;
    bool _needsAttention;
    bool _needsUpdate;
    ICommand? _selectCommand;
    Bitmap? _thumbnail;
    ProjectType _type;
    int _updateCount;

    /// <summary>
    ///     Initializes a new instance of the ProjectModel class.
    /// </summary>
    /// <param name="type">The type of project.</param>
    /// <param name="name">The display name.</param>
    /// <param name="projectPath">Path to the .csproj file.</param>
    /// <param name="lastReferenced">When the project was last accessed.</param>
    /// <param name="shell">Shell service for dialogs.</param>
    /// <param name="projectService">Project service for operations (optional for design mode).</param>
    public ProjectModel(ProjectType type, string name, CanonicalPath projectPath, DateTimeOffset lastReferenced, IShell shell, IProjectService? projectService = null)
    {
        ProjectPath = projectPath;
        _lastReferenced = lastReferenced;
        _shell = shell;
        _projectService = projectService;
        _name = name;
        _type = type;
        _deleteCommand = new AsyncRelayCommand(DeleteAsync, CanDelete);
        _removeFromHubCommand = new AsyncRelayCommand(RemoveFromHubAsync, CanDelete);

        // Start async thumbnail loading
        _ = LoadThumbnailAsync();
    }

    /// <summary>
    ///     Gets the canonical path to the .csproj file.
    /// </summary>
    public CanonicalPath ProjectPath { get; }

    /// <summary>
    ///     Gets or sets whether this project is currently selected in the UI.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    /// <summary>
    ///     Gets or sets whether this project requires user attention (e.g., configuration issues).
    /// </summary>
    public bool NeedsAttention
    {
        get => _needsAttention;
        set => SetProperty(ref _needsAttention, value);
    }

    /// <summary>
    ///     Gets or sets the command to execute when this project is selected.
    /// </summary>
    public ICommand? SelectCommand
    {
        get => _selectCommand;
        set => SetProperty(ref _selectCommand, value);
    }

    /// <summary>
    ///     Gets the type of project (Programmable Block or Mod).
    /// </summary>
    public ProjectType Type
    {
        get => _type;
        private set => SetProperty(ref _type, value);
    }

    /// <summary>
    ///     Gets the display name of the project.
    /// </summary>
    public string Name
    {
        get => _name;
        private set => SetProperty(ref _name, value);
    }

    /// <summary>
    ///     Gets the timestamp when the project was last opened or modified.
    /// </summary>
    public DateTimeOffset LastReferenced
    {
        get => _lastReferenced;
        private set => SetProperty(ref _lastReferenced, value);
    }

    /// <summary>
    ///     Gets or sets whether this project has unsaved configuration changes.
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        set => SetProperty(ref _hasUnsavedChanges, value);
    }

    /// <summary>
    ///     Gets or sets the number of outdated MDK packages in this project.
    /// </summary>
    public int UpdateCount
    {
        get => _updateCount;
        set => SetProperty(ref _updateCount, value);
    }

    /// <summary>
    ///     Gets or sets whether MDK package updates are available for this project.
    /// </summary>
    public bool NeedsUpdate
    {
        get => _needsUpdate;
        set => SetProperty(ref _needsUpdate, value);
    }

    /// <summary>
    ///     Gets the project thumbnail image (thumb.png or thumb.jpg) if available.
    /// </summary>
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set => SetProperty(ref _thumbnail, value);
    }

    /// <summary>
    ///     Gets the command to permanently delete this project from disk.
    /// </summary>
    public ICommand DeleteCommand => _deleteCommand;

    /// <summary>
    ///     Gets the command to remove this project from Hub (but keep files on disk).
    /// </summary>
    public ICommand RemoveFromHubCommand => _removeFromHubCommand;

    /// <summary>
    ///     Determines whether the project can be deleted.
    /// </summary>
    /// <returns>Always returns true.</returns>
    public bool CanDelete() => true;

    /// <summary>
    ///     Removes the project from Hub without deleting files from disk.
    /// </summary>
    public async Task RemoveFromHubAsync()
    {
        if (!CanDelete())
            return;
        var result = await _shell.ShowOverlayAsync(
            new ConfirmationMessage
            {
                Title = "Remove Project",
                Message = $"Remove \"{Name}\" from MDK Hub?\n\nThe project files will remain on disk. You can re-add it later by opening the project.",
                OkText = "Remove from Hub",
                CancelText = "Cancel"
            });
        if (!result)
            return;

        try
        {
            // Remove from registry only
            if (!ProjectPath.IsEmpty())
                _projectService?.RemoveProject(ProjectPath);
        }
        catch (Exception ex)
        {
            await _shell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Remove Failed",
                Message = $"Failed to remove project from Hub: {ex.Message}",
                OkText = "OK",
                CancelText = "Close"
            });
        }
    }

    /// <summary>
    ///     Permanently deletes the project directory and files from disk.
    /// </summary>
    public async Task DeleteAsync()
    {
        if (!CanDelete())
            return;
        var result = await _shell.ShowOverlayAsync(
            new KeyPhraseValidationMessage
            {
                Title = "Delete Project",
                Message = $"Delete project \"{Name}\"? This permanently deletes the C# project itself, not only its hub registration. This action cannot be undone.",
                KeyPhraseWatermark = $"Type \"{Name}\" here to confirm",
                RequiredKeyPhrase = Name,
                OkText = "Delete"
            });
        if (!result)
            return;

        try
        {
            // Delete the project directory
            var projectDirectory = Path.GetDirectoryName(ProjectPath.Value);
            if (!string.IsNullOrEmpty(projectDirectory) && Directory.Exists(projectDirectory))
                Directory.Delete(projectDirectory, true);

            // Remove from registry
            if (!ProjectPath.IsEmpty())
                _projectService?.RemoveProject(ProjectPath);
        }
        catch (Exception ex)
        {
            await _shell.ShowOverlayAsync(new ConfirmationMessage
            {
                Title = "Delete Failed",
                Message = $"Failed to delete project: {ex.Message}",
                OkText = "OK",
                CancelText = "Close"
            });
        }
    }

    /// <summary>
    ///     Determines whether this project matches the specified search and type filters.
    /// </summary>
    /// <param name="searchText">Text to search in the project name.</param>
    /// <param name="mustBeMod">If true, only Mod projects pass the filter.</param>
    /// <param name="mustBeScript">If true, only ProgrammableBlock projects pass the filter.</param>
    /// <returns>True if the project matches all specified filters.</returns>
    public bool MatchesFilter(string searchText, bool mustBeMod, bool mustBeScript)
    {
        if (mustBeMod && Type != ProjectType.Mod)
            return false;
        if (mustBeScript && Type != ProjectType.ProgrammableBlock)
            return false;
        if (!string.IsNullOrWhiteSpace(searchText) && !Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    /// <summary>
    ///     Updates this model's properties from a ProjectInfo without losing UI state.
    /// </summary>
    /// <param name="projectInfo">The source ProjectInfo containing updated values.</param>
    public void UpdateFromProjectInfo(ProjectInfo projectInfo)
    {
        Name = projectInfo.Name;
        Type = projectInfo.Type;
        LastReferenced = projectInfo.LastReferenced;
        // Note: ProjectPath, IsSelected, NeedsAttention, HasUnsavedChanges are NOT updated - those preserve UI state
    }

    async Task LoadThumbnailAsync()
    {
        try
        {
            if (ProjectPath.IsEmpty())
                return;

            var projectDir = Path.GetDirectoryName(ProjectPath.Value);
            if (string.IsNullOrEmpty(projectDir))
                return;

            // Throttle concurrent loads
            await _thumbnailLoadSemaphore.WaitAsync();
            try
            {
                // All file I/O on background thread
                await Task.Run(() =>
                {
                    // Check for thumb.png first, then thumb.jpg
                    string? thumbPath = null;
                    var pngPath = Path.Combine(projectDir, "thumb.png");
                    var jpgPath = Path.Combine(projectDir, "thumb.jpg");

                    if (File.Exists(pngPath))
                        thumbPath = pngPath;
                    else if (File.Exists(jpgPath))
                        thumbPath = jpgPath;

                    if (thumbPath == null)
                        return;

                    // Load bitmap
                    using var stream = File.OpenRead(thumbPath);
                    using var originalBitmap = new Bitmap(stream);

                    // Resize to reasonable max (2x display size for retina) if necessary
                    const int maxWidth = 170; // 2x display width of 85px
                    const int maxHeight = 112; // 2x display height of 56px

                    Bitmap finalBitmap;

                    // Calculate if resizing is needed
                    var scaleX = (double)maxWidth / originalBitmap.PixelSize.Width;
                    var scaleY = (double)maxHeight / originalBitmap.PixelSize.Height;
                    var scale = Math.Min(scaleX, scaleY);

                    if (scale < 1.0)
                    {
                        // Resize - image is larger than needed
                        var newWidth = (int)(originalBitmap.PixelSize.Width * scale);
                        var newHeight = (int)(originalBitmap.PixelSize.Height * scale);
                        finalBitmap = originalBitmap.CreateScaledBitmap(new PixelSize(newWidth, newHeight));
                    }
                    else
                    {
                        // Create copy - image is already small enough
                        finalBitmap = new Bitmap(stream);
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    // Update property on UI thread
                    Dispatcher.UIThread.Post(() => Thumbnail = finalBitmap);
                });
            }
            finally
            {
                _thumbnailLoadSemaphore.Release();
            }
        }
        catch
        {
            // Silently fail - thumbnail is optional
        }
    }
}
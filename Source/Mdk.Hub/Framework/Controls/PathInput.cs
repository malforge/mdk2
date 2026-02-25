using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A templated control for path input with browse and optional reset functionality.
///     Supports manual entry, folder picker, and reset to default value.
/// </summary>
public class PathInput : PathTextInputBase
{
    /// <summary>
    /// Open Directory Browser on Click.
    /// </summary>
    protected override async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false,
            SuggestedStartLocation = string.IsNullOrWhiteSpace(Path)
                ? null
                : await topLevel.StorageProvider.TryGetFolderFromPathAsync(Path)
        });

        if (result.Count > 0)
            Path = result[0].Path.LocalPath;
    }

    /// <summary>
    ///     Determines if the current platform is Windows.
    ///     Virtual to allow testing of platform-specific validation logic.
    /// </summary>
    protected virtual bool IsWindowsPlatform() => OperatingSystem.IsWindows();

    /// <summary>
    ///     Normalize Path on current platform
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    protected override string NormalizeInput(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        var normalized = path.Trim();

        if (IsWindowsPlatform())
        {
            // Replace forward slashes with backslashes
            normalized = normalized.Replace('/', '\\');

            // Collapse consecutive backslashes
            while (normalized.Contains("\\\\"))
                normalized = normalized.Replace("\\\\", "\\");

            // Remove trailing backslash (unless it's a root like C:\)
            if (normalized.Length > 3 && normalized.EndsWith('\\'))
                normalized = normalized.TrimEnd('\\');

            // Remove trailing spaces and periods (Windows doesn't allow)
            normalized = normalized.TrimEnd(' ', '.');
        }
        else // Unix/Linux/Mac
        {
            // Collapse consecutive forward slashes
            while (normalized.Contains("//"))
                normalized = normalized.Replace("//", "/");

            // Remove trailing slash (unless it's root /)
            if (normalized.Length > 1 && normalized.EndsWith('/'))
                normalized = normalized.TrimEnd('/');
        }

        return normalized;
    }

    /// <summary>
    ///     Checks if Path is valid on current platform
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    protected override bool IsValidPathFormat(string? path)
    {
        // Empty is only valid if there's no default (or default is also empty)
        if (string.IsNullOrEmpty(path))
            return string.IsNullOrEmpty(DefaultPath);

        // Universal invalid characters: null, <, >, ", |, ?, *, and control characters
        var invalidChars = new[] { '\0', '<', '>', '"', '|', '?', '*' };
        if (path.Any(c => invalidChars.Contains(c) || char.IsControl(c)))
            return false;

        // Length check: 4096 chars max
        // This is the Unix PATH_MAX standard and is reasonable for cross-platform use.
        // Windows traditionally had 260 (MAX_PATH) but supports much longer with long path support.
        // This limit prevents abuse while allowing legitimate deep directory structures.
        if (path.Length > 4096)
            return false;

        // Check for consecutive separators (should have been normalized already)
        if (path.Contains("//") || path.Contains("\\\\"))
            return false;

        if (IsWindowsPlatform())
        {
            // Colon only allowed at position 1 (drive letter like C:) and only once
            var firstColonIndex = path.IndexOf(':');
            if (firstColonIndex != -1)
            {
                if (firstColonIndex != 1)
                    return false;
                
                // Check for additional colons after the first one
                if (path.IndexOf(':', firstColonIndex + 1) != -1)
                    return false;
            }

            // Can't end with space or period (should have been normalized already)
            if (path.EndsWith(' ') || path.EndsWith('.'))
                return false;

            // Check for reserved names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)
            var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5",
                "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
            
            var segments = path.Split('\\', '/');
            foreach (var segment in segments)
            {
                var name = segment.Split('.')[0].ToUpperInvariant();
                if (reservedNames.Contains(name))
                    return false;
            }
        }
        else // Unix/Linux/Mac
        {
            // Only null byte is invalid (already checked above)
        }

        return true;
    }

    /// <summary>
    /// Validates that the selected path exists when existence checks are enabled.
    /// </summary>
    protected override bool PathExists(string normalizedPath)
    {
        return Directory.Exists(normalizedPath);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;


namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A templated control for path input with browse and optional reset functionality.
///     Supports manual entry, file picker, and reset to default value.
/// </summary>
public class FileInput : PathInput
{
    /// <summary>
    ///     Defines the <see cref="TypeName" /> property.
    /// </summary>
    public static readonly StyledProperty<string> TypeNameProperty =
        AvaloniaProperty.Register<FileInput, string>(
            nameof(TypeName), "File");

    /// <summary>
    ///     Name of the Type filter applied to File Picker 
    /// </summary>
    public string TypeName
    {
        get => GetValue(TypeNameProperty);
        set => SetValue(TypeNameProperty, value);
    }

    /// <summary>
    ///     Defines the <see cref="Patterns" /> property.
    /// </summary>
    public static readonly StyledProperty<string> PatternsProperty =
        AvaloniaProperty.Register<FileInput, string>(
            nameof(Patterns), "*");

    /// <summary>
    ///     Pattern-based filter applied to File Picker
    ///     use ';' for multiples patterns. ex: "*.png;*.jpeg;malware*;cool-image.bmp"
    /// </summary>
    public string Patterns
    {
        get => GetValue(PatternsProperty);
        set => SetValue(PatternsProperty, value);
    }
    
    /// <summary>
    ///     Open File Browser on Click
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override async void OnBrowseClick(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null)
            return;
        
        var fileTypes = new FilePickerFileType(TypeName)
        {
            Patterns = Patterns.Split(";", StringSplitOptions.RemoveEmptyEntries).AsReadOnly()
        };

        var result = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = "Select File",
            AllowMultiple = false,
            SuggestedFileName = Path,
            FileTypeFilter = [fileTypes],
            SuggestedStartLocation = string.IsNullOrWhiteSpace(Path)
                ? null
                : await topLevel.StorageProvider.TryGetFolderFromPathAsync(Path)
        });

        if (result.Count > 0)
            Path = result[0].Path.LocalPath;
    }

    /// <summary>
    /// Validates that the selected file exists when existence checks are enabled.
    /// </summary>
    protected override bool PathExists(string normalizedPath)
    {
        return File.Exists(normalizedPath);
    }

    /// <summary>
    /// Error shown when file existence is required but missing.
    /// </summary>
    protected override string MissingPathMessage => "File does not exist";
}

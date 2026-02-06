using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Mdk.Hub.Controls;

/// <summary>
/// A control that provides a text box and browse button for selecting a folder path.
/// </summary>
public partial class FolderBrowserControl : UserControl
{
    /// <summary>
    /// Defines the Path property.
    /// </summary>
    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<FolderBrowserControl, string>(
            nameof(Path),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the Watermark property.
    /// </summary>
    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<FolderBrowserControl, string>(nameof(Watermark), "Select a folder...");

    /// <summary>
    /// Defines the HasError property.
    /// </summary>
    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<FolderBrowserControl, bool>(nameof(HasError), false);

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderBrowserControl"/> class.
    /// </summary>
    public FolderBrowserControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the selected folder path.
    /// </summary>
    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets the watermark text displayed when the path is empty.
    /// </summary>
    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the control should display an error state.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    async void OnBrowseClick(object? sender, RoutedEventArgs e)
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
}

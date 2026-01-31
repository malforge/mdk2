using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Mdk.Hub.Controls;

public partial class FolderBrowserControl : UserControl
{
    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<FolderBrowserControl, string>(
            nameof(Path),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> WatermarkProperty =
        AvaloniaProperty.Register<FolderBrowserControl, string>(nameof(Watermark), "Select a folder...");

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<FolderBrowserControl, bool>(nameof(HasError), false);

    public FolderBrowserControl()
    {
        InitializeComponent();
    }

    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public string Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

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
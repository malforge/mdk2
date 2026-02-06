using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Platform.Storage;

namespace Mdk.Hub.Controls;

/// <summary>
/// A control that allows users to select a path using either "auto" mode, a folder picker, or a custom path.
/// </summary>
public partial class PathSelectorControl : UserControl
{
    /// <summary>
    /// Defines the Path property.
    /// </summary>
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<PathSelectorControl, string?>(nameof(Path), "auto", defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the DefaultPath property.
    /// </summary>
    public static readonly StyledProperty<string?> DefaultPathProperty =
        AvaloniaProperty.Register<PathSelectorControl, string?>(nameof(DefaultPath));

    /// <summary>
    /// Defines the Watermark property.
    /// </summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PathSelectorControl, string?>(nameof(Watermark));

    /// <summary>
    /// Defines the DefaultText property.
    /// </summary>
    public static readonly StyledProperty<string> DefaultTextProperty =
        AvaloniaProperty.Register<PathSelectorControl, string>(nameof(DefaultText), "Default");

    /// <summary>
    /// Defines the SelectedIndex property.
    /// </summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<PathSelectorControl, int>(nameof(SelectedIndex));

    private ComboBox? _comboBox;
    private bool _updatingSelection;

    /// <summary>
    /// Initializes a new instance of the <see cref="PathSelectorControl"/> class.
    /// </summary>
    public PathSelectorControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the selected path value ("auto", a folder path, or empty).
    /// </summary>
    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets the default path used as a suggested start location when browsing.
    /// </summary>
    public string? DefaultPath
    {
        get => GetValue(DefaultPathProperty);
        set => SetValue(DefaultPathProperty, value);
    }

    /// <summary>
    /// Gets or sets the watermark text displayed when no path is selected.
    /// </summary>
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    /// <summary>
    /// Gets or sets the text displayed for the default/auto option.
    /// </summary>
    public string DefaultText
    {
        get => GetValue(DefaultTextProperty);
        set => SetValue(DefaultTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the currently selected index in the ComboBox.
    /// </summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Called when the control's template is applied, initializes the ComboBox.
    /// </summary>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Unwire old event if exists
        if (_comboBox != null)
            _comboBox.SelectionChanged -= OnComboBoxSelectionChanged;

        // Find and wire up the ComboBox
        _comboBox = this.FindControl<ComboBox>("PART_ComboBox");
        if (_comboBox != null)
        {
            // Populate items in code since bindings don't work in Items collection
            _comboBox.Items.Clear();
            _comboBox.Items.Add(new ComboBoxItem { Content = DefaultText });
            _comboBox.Items.Add(new ComboBoxItem { Content = "Find Folder..." });

            _comboBox.SelectionChanged += OnComboBoxSelectionChanged;
            UpdateSelectionFromPath();
        }
    }

    /// <summary>
    /// Called when a property value changes, handles Path and DefaultText property updates.
    /// </summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == PathProperty && !_updatingSelection)
            UpdateSelectionFromPath();
        else if (change.Property == DefaultTextProperty && _comboBox != null)
        {
            // Update the first item when DefaultText changes
            if (_comboBox.Items.Count > 0 && _comboBox.Items[0] is ComboBoxItem item)
                item.Content = DefaultText;
        }
    }

    private void UpdateSelectionFromPath()
    {
        if (_comboBox == null)
            return;

        _updatingSelection = true;
        try
        {
            // Remove old custom path if exists
            while (_comboBox.Items.Count > 2)
                _comboBox.Items.RemoveAt(2);

            if (string.IsNullOrWhiteSpace(Path))
            {
                // Empty path - let ComboBox show placeholder text
                SelectedIndex = -1; // No selection
            }
            else if (Path == "auto")
                SelectedIndex = 0; // Auto
            else
            {
                // Add the custom path item
                var pathItem = new ComboBoxItem
                {
                    Content = Path
                };
                ToolTip.SetTip(pathItem, Path);
                _comboBox.Items.Add(pathItem);
                SelectedIndex = 2;
            }
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private async void OnComboBoxSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_updatingSelection || _comboBox == null)
            return;

        var selectedIndex = _comboBox.SelectedIndex;

        // Ignore deselection (when we set SelectedIndex = -1)
        if (selectedIndex < 0)
            return;

        if (selectedIndex == 0)
        {
            // Auto selected
            _updatingSelection = true;
            try
            {
                Path = "auto";
            }
            finally
            {
                _updatingSelection = false;
            }
        }
        else if (selectedIndex == 1)
        {
            // Find Folder... selected - open dialog
            await BrowseFolderAsync();
        }
        // selectedIndex == 2 means custom path already selected, nothing to do
    }

    private async Task BrowseFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider == null || _comboBox == null)
        {
            // Revert selection if we can't open dialog
            UpdateSelectionFromPath();
            return;
        }

        var options = new FolderPickerOpenOptions
        {
            Title = "Select Output Folder",
            AllowMultiple = false
        };

        // Determine suggested start location
        string? suggestedPath = null;

        // 1. If current path is valid and not "auto", use it
        if (!string.IsNullOrWhiteSpace(Path) && Path != "auto" && Directory.Exists(Path))
            suggestedPath = Path;
        // 2. Otherwise, try the default path if it exists
        else if (!string.IsNullOrWhiteSpace(DefaultPath) && Directory.Exists(DefaultPath))
            suggestedPath = DefaultPath;

        if (suggestedPath != null)
        {
            try
            {
                var folder = await topLevel.StorageProvider.TryGetFolderFromPathAsync(new Uri(System.IO.Path.GetFullPath(suggestedPath)));
                if (folder != null)
                    options.SuggestedStartLocation = folder;
            }
            catch
            {
                // Ignore if path is invalid
            }
        }

        var result = await topLevel.StorageProvider.OpenFolderPickerAsync(options);

        if (result.Count > 0)
        {
            var selectedPath = result[0].Path.LocalPath;
            Path = selectedPath;
            // UpdateSelectionFromPath will be called via PropertyChanged
        }
        else
        {
            // User cancelled, revert to previous selection
            UpdateSelectionFromPath();
        }
    }
}

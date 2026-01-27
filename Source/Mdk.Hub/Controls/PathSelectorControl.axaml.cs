using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Platform.Storage;

namespace Mdk.Hub.Controls;

public partial class PathSelectorControl : UserControl
{
    public static readonly StyledProperty<string?> PathProperty =
        AvaloniaProperty.Register<PathSelectorControl, string?>(nameof(Path), "auto", defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> DefaultPathProperty =
        AvaloniaProperty.Register<PathSelectorControl, string?>(nameof(DefaultPath));

    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PathSelectorControl, string?>(nameof(Watermark));

    public static readonly StyledProperty<string> DefaultTextProperty =
        AvaloniaProperty.Register<PathSelectorControl, string>(nameof(DefaultText), "Default");

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<PathSelectorControl, int>(nameof(SelectedIndex));

    private ComboBox? _comboBox;
    private bool _updatingSelection;

    public PathSelectorControl()
    {
        InitializeComponent();
    }

    public string? Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public string? DefaultPath
    {
        get => GetValue(DefaultPathProperty);
        set => SetValue(DefaultPathProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public string DefaultText
    {
        get => GetValue(DefaultTextProperty);
        set => SetValue(DefaultTextProperty, value);
    }

    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

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
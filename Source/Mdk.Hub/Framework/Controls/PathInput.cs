using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
///     A templated control for path input with browse and optional reset functionality.
///     Supports manual entry, folder picker, and reset to default value.
/// </summary>
public class PathInput : TemplatedControl, ISupportValidation
{
    /// <summary>
    ///     Defines the <see cref="Path" /> property.
    /// </summary>
    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<PathInput, string>(
            nameof(Path),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    ///     Defines the <see cref="DefaultPath" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> DefaultPathProperty =
        AvaloniaProperty.Register<PathInput, string?>(nameof(DefaultPath));

    /// <summary>
    ///     Defines the <see cref="CanReset" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanResetProperty =
        AvaloniaProperty.Register<PathInput, bool>(nameof(CanReset), false);

    /// <summary>
    ///     Defines the <see cref="ResetTooltip" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> ResetTooltipProperty =
        AvaloniaProperty.Register<PathInput, string?>(nameof(ResetTooltip));

    /// <summary>
    ///     Defines the <see cref="Watermark" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PathInput, string?>(nameof(Watermark));

    /// <summary>
    ///     Defines the <see cref="HasError" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<PathInput, bool>(nameof(HasError), false);

    /// <summary>
    ///     Defines the <see cref="ValidationError" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValidationErrorProperty =
        AvaloniaProperty.Register<PathInput, string?>(nameof(ValidationError));

    /// <summary>
    ///     Defines the <see cref="ResolvedResetTooltip" /> property.
    /// </summary>
    public static readonly StyledProperty<string> ResolvedResetTooltipProperty =
        AvaloniaProperty.Register<PathInput, string>(nameof(ResolvedResetTooltip), "Reset to default");

    /// <summary>
    ///     Defines the <see cref="ResolvedWatermark" /> property.
    /// </summary>
    public static readonly StyledProperty<string> ResolvedWatermarkProperty =
        AvaloniaProperty.Register<PathInput, string>(nameof(ResolvedWatermark), "Enter path or leave empty for default");

    /// <summary>
    ///     Defines the <see cref="CanResetToDefault" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanResetToDefaultProperty =
        AvaloniaProperty.Register<PathInput, bool>(nameof(CanResetToDefault), false);

    Button? _browseButton;
    Button? _resetButton;
    TextBox? _textBox;

    /// <summary>
    ///     Gets or sets the current path value.
    /// </summary>
    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    ///     Gets or sets the default path value used when resetting.
    /// </summary>
    public string? DefaultPath
    {
        get => GetValue(DefaultPathProperty);
        set => SetValue(DefaultPathProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether the reset button should be shown.
    /// </summary>
    public bool CanReset
    {
        get => GetValue(CanResetProperty);
        set => SetValue(CanResetProperty, value);
    }

    /// <summary>
    ///     Gets or sets the tooltip text for the reset button.
    /// </summary>
    public string? ResetTooltip
    {
        get => GetValue(ResetTooltipProperty);
        set => SetValue(ResetTooltipProperty, value);
    }

    /// <summary>
    ///     Gets or sets the watermark text displayed when the path is empty.
    /// </summary>
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    /// <summary>
    ///     Gets or sets whether the control should display an error state.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the validation error message, or null if validation passes.
    /// </summary>
    public string? ValidationError
    {
        get => GetValue(ValidationErrorProperty);
        set => SetValue(ValidationErrorProperty, value);
    }

    /// <summary>
    ///     Gets the resolved reset tooltip with fallback logic.
    /// </summary>
    public string ResolvedResetTooltip
    {
        get => GetValue(ResolvedResetTooltipProperty);
        private set => SetValue(ResolvedResetTooltipProperty, value);
    }

    /// <summary>
    ///     Gets the resolved watermark with fallback logic.
    /// </summary>
    public string ResolvedWatermark
    {
        get => GetValue(ResolvedWatermarkProperty);
        private set => SetValue(ResolvedWatermarkProperty, value);
    }

    /// <summary>
    ///     Gets whether the reset button should be enabled.
    /// </summary>
    public bool CanResetToDefault
    {
        get => GetValue(CanResetToDefaultProperty);
        private set => SetValue(CanResetToDefaultProperty, value);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Unwire old events if they exist
        if (_textBox != null)
        {
            _textBox.TextChanged -= OnTextChanged;
            _textBox.LostFocus -= OnTextBoxLostFocus;
            _textBox.KeyDown -= OnTextBoxKeyDown;
        }
        if (_browseButton != null)
            _browseButton.Click -= OnBrowseClick;
        if (_resetButton != null)
            _resetButton.Click -= OnResetClick;

        // Find and wire up template parts
        _textBox = e.NameScope.Find<TextBox>("PART_TextBox");
        if (_textBox != null)
        {
            _textBox.Text = Path;
            _textBox.TextChanged += OnTextChanged;
            _textBox.LostFocus += OnTextBoxLostFocus;
            _textBox.KeyDown += OnTextBoxKeyDown;
        }

        _browseButton = e.NameScope.Find<Button>("PART_BrowseButton");
        if (_browseButton != null)
            _browseButton.Click += OnBrowseClick;

        _resetButton = e.NameScope.Find<Button>("PART_ResetButton");
        if (_resetButton != null)
            _resetButton.Click += OnResetClick;

        UpdateResolvedProperties();
        UpdatePseudoClasses();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HasErrorProperty)
            UpdatePseudoClasses();
        else if (change.Property == PathProperty)
        {
            // Update TextBox when Path changes externally (e.g., from ViewModel)
            if (_textBox != null && _textBox.Text != Path)
                _textBox.Text = Path;
            UpdateResolvedProperties();
        }
        else if (change.Property == DefaultPathProperty || 
                 change.Property == ResetTooltipProperty || change.Property == WatermarkProperty ||
                 change.Property == CanResetProperty)
            UpdateResolvedProperties();
    }

    void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":error", HasError);
    }

    void UpdateResolvedProperties()
    {
        // Update reset tooltip with fallback logic
        if (!string.IsNullOrWhiteSpace(ResetTooltip))
            ResolvedResetTooltip = ResetTooltip;
        else if (!string.IsNullOrWhiteSpace(DefaultPath))
            ResolvedResetTooltip = $"Reset to '{DefaultPath}'";
        else
            ResolvedResetTooltip = "Reset to default";

        // Update watermark with fallback logic
        if (!string.IsNullOrWhiteSpace(Watermark))
            ResolvedWatermark = Watermark;
        else
            ResolvedWatermark = "Enter path or leave empty for default";

        // Update reset button enabled state - only allow reset if:
        // 1. CanReset is true (user allows reset functionality)
        // 2. Path differs from DefaultPath (there's something to reset)
        CanResetToDefault = CanReset && (Path != DefaultPath);
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

    void OnResetClick(object? sender, RoutedEventArgs e)
    {
        Path = DefaultPath ?? string.Empty;
    }

    /// <summary>
    ///     Determines if the current platform is Windows.
    ///     Virtual to allow testing of platform-specific validation logic.
    /// </summary>
    protected virtual bool IsWindowsPlatform() => OperatingSystem.IsWindows();

    void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_textBox == null)
            return;

        // Real-time validation against normalized path
        var text = _textBox.Text ?? string.Empty;
        
        // Skip validation if it's the default value
        if (text == DefaultPath)
        {
            HasError = false;
            ValidationError = null;
            return;
        }

        // Validate the normalized version
        var normalized = NormalizePath(text);
        var isValid = IsValidPathFormat(normalized);
        HasError = !isValid;
        ValidationError = isValid ? null : "Invalid path format";
    }

    void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        CommitText();
    }

    void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            CommitText();
    }

    void CommitText()
    {
        if (_textBox == null)
            return;

        var text = _textBox.Text ?? string.Empty;
        
        // Always allow default value without normalization
        if (text == DefaultPath)
        {
            Path = text;
            HasError = false;
            ValidationError = null;
            return;
        }

        // Normalize and validate
        var normalized = NormalizePath(text);
        if (IsValidPathFormat(normalized))
        {
            Path = normalized;
            _textBox.Text = normalized; // Update TextBox with cleaned version
            HasError = false;
            ValidationError = null;
        }
        else
        {
            // Invalid path - don't commit but keep error state
            HasError = true;
            ValidationError = "Invalid path format";
        }
    }

    string NormalizePath(string? path)
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

    bool IsValidPathFormat(string? path)
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
}

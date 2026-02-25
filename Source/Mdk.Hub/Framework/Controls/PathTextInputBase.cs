using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mdk.Hub.Framework.Controls;

/// <summary>
/// Base control for path-like inputs that own a textbox, browse button, and reset button.
/// </summary>
public abstract class PathTextInputBase : TemplatedControl, ISupportValidation
{
    /// <summary>
    /// Defines the <see cref="Path" /> property.
    /// </summary>
    public static readonly StyledProperty<string> PathProperty =
        AvaloniaProperty.Register<PathTextInputBase, string>(
            nameof(Path),
            string.Empty,
            defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="DefaultPath" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> DefaultPathProperty =
        AvaloniaProperty.Register<PathTextInputBase, string?>(nameof(DefaultPath));

    /// <summary>
    /// Defines the <see cref="CanReset" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanResetProperty =
        AvaloniaProperty.Register<PathTextInputBase, bool>(nameof(CanReset), false);

    /// <summary>
    /// Defines the <see cref="ResetTooltip" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> ResetTooltipProperty =
        AvaloniaProperty.Register<PathTextInputBase, string?>(nameof(ResetTooltip));

    /// <summary>
    /// Defines the <see cref="Watermark" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PathTextInputBase, string?>(nameof(Watermark));

    /// <summary>
    /// Defines the <see cref="HasError" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<PathTextInputBase, bool>(nameof(HasError), false);

    /// <summary>
    /// Defines the <see cref="ValidationError" /> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValidationErrorProperty =
        AvaloniaProperty.Register<PathTextInputBase, string?>(nameof(ValidationError));

    /// <summary>
    /// Defines the <see cref="ResolvedResetTooltip" /> property.
    /// </summary>
    public static readonly StyledProperty<string> ResolvedResetTooltipProperty =
        AvaloniaProperty.Register<PathTextInputBase, string>(nameof(ResolvedResetTooltip), "Reset to default");

    /// <summary>
    /// Defines the <see cref="ResolvedWatermark" /> property.
    /// </summary>
    public static readonly StyledProperty<string> ResolvedWatermarkProperty =
        AvaloniaProperty.Register<PathTextInputBase, string>(nameof(ResolvedWatermark), "Enter path or leave empty for default");

    /// <summary>
    /// Defines the <see cref="CanResetToDefault" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanResetToDefaultProperty =
        AvaloniaProperty.Register<PathTextInputBase, bool>(nameof(CanResetToDefault), false);

    /// <summary>
    /// Defines the <see cref="AllowNonExisting" /> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowNonExistingProperty =
        AvaloniaProperty.Register<PathTextInputBase, bool>(nameof(AllowNonExisting), true);

    Button? _browseButton;
    Button? _resetButton;
    TextBox? _textBox;

    /// <summary>
    /// Gets or sets the current input value.
    /// </summary>
    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    /// <summary>
    /// Gets or sets the default value used by reset operations.
    /// </summary>
    public string? DefaultPath
    {
        get => GetValue(DefaultPathProperty);
        set => SetValue(DefaultPathProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the reset button is visible.
    /// </summary>
    public bool CanReset
    {
        get => GetValue(CanResetProperty);
        set => SetValue(CanResetProperty, value);
    }

    /// <summary>
    /// Gets or sets the reset button tooltip text.
    /// </summary>
    public string? ResetTooltip
    {
        get => GetValue(ResetTooltipProperty);
        set => SetValue(ResetTooltipProperty, value);
    }

    /// <summary>
    /// Gets or sets the textbox watermark text.
    /// </summary>
    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the control is currently in an error state.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    /// <summary>
    /// Gets or sets the current validation message.
    /// </summary>
    public string? ValidationError
    {
        get => GetValue(ValidationErrorProperty);
        set => SetValue(ValidationErrorProperty, value);
    }

    /// <summary>
    /// Gets the resolved reset tooltip text with fallback logic.
    /// </summary>
    public string ResolvedResetTooltip
    {
        get => GetValue(ResolvedResetTooltipProperty);
        private set => SetValue(ResolvedResetTooltipProperty, value);
    }

    /// <summary>
    /// Gets the resolved watermark text with fallback logic.
    /// </summary>
    public string ResolvedWatermark
    {
        get => GetValue(ResolvedWatermarkProperty);
        private set => SetValue(ResolvedWatermarkProperty, value);
    }

    /// <summary>
    /// Gets whether resetting to default is currently possible.
    /// </summary>
    public bool CanResetToDefault
    {
        get => GetValue(CanResetToDefaultProperty);
        private set => SetValue(CanResetToDefaultProperty, value);
    }

    /// <summary>
    /// Gets or sets whether non-existing targets are considered valid.
    /// </summary>
    public bool AllowNonExisting
    {
        get => GetValue(AllowNonExistingProperty);
        set => SetValue(AllowNonExistingProperty, value);
    }

    /// <summary>
    /// Gets the current textbox text value.
    /// </summary>
    protected string CurrentText => _textBox?.Text ?? string.Empty;

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

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
            if (_textBox != null && _textBox.Text != Path)
                _textBox.Text = Path;
            UpdateResolvedProperties();
        }
        else if (change.Property == DefaultPathProperty ||
                 change.Property == ResetTooltipProperty ||
                 change.Property == WatermarkProperty ||
                 change.Property == CanResetProperty)
            UpdateResolvedProperties();
    }

    /// <summary>
    /// Normalizes user-entered text before format and existence checks.
    /// </summary>
    protected virtual string NormalizeInput(string? path)
    {
        return path?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Returns whether the normalized input has a valid format.
    /// </summary>
    protected abstract bool IsValidPathFormat(string? path);

    /// <summary>
    /// Returns whether the normalized input exists in storage.
    /// </summary>
    protected virtual bool PathExists(string normalizedPath)
    {
        return true;
    }

    /// <summary>
    /// Gets the error message shown when a missing path is rejected.
    /// </summary>
    protected virtual string MissingPathMessage => "Path does not exist";

    /// <summary>
    /// Gets the error message shown for invalid input format.
    /// </summary>
    protected virtual string InvalidPathMessage => "Invalid path format";

    /// <summary>
    /// Updates textbox content without bypassing template-part checks.
    /// </summary>
    protected void SetCurrentText(string value)
    {
        if (_textBox != null)
            _textBox.Text = value;
    }

    /// <summary>
    /// Commits textbox content into <see cref="Path" /> if validation succeeds.
    /// </summary>
    protected virtual void CommitText()
    {
        var text = CurrentText;

        if (text == DefaultPath)
        {
            Path = text;
            HasError = false;
            ValidationError = null;
            return;
        }

        var normalized = NormalizeInput(text);
        if (!IsValidPathFormat(normalized))
        {
            HasError = true;
            ValidationError = InvalidPathMessage;
            return;
        }

        if (!AllowNonExisting && !string.IsNullOrEmpty(normalized) && !PathExists(normalized))
        {
            HasError = true;
            ValidationError = MissingPathMessage;
            return;
        }

        Path = normalized;
        SetCurrentText(normalized);
        HasError = false;
        ValidationError = null;
    }

    /// <summary>
    /// Opens a picker appropriate for the derived control.
    /// </summary>
    protected abstract void OnBrowseClick(object? sender, RoutedEventArgs e);

    void OnResetClick(object? sender, RoutedEventArgs e)
    {
        Path = DefaultPath ?? string.Empty;
    }

    void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = CurrentText;
        if (text == DefaultPath)
        {
            HasError = false;
            ValidationError = null;
            return;
        }

        var normalized = NormalizeInput(text);
        var isValid = IsValidPathFormat(normalized);
        HasError = !isValid;
        ValidationError = isValid ? null : InvalidPathMessage;
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

    void UpdatePseudoClasses()
    {
        PseudoClasses.Set(":error", HasError);
    }

    void UpdateResolvedProperties()
    {
        if (!string.IsNullOrWhiteSpace(ResetTooltip))
            ResolvedResetTooltip = ResetTooltip;
        else if (!string.IsNullOrWhiteSpace(DefaultPath))
            ResolvedResetTooltip = $"Reset to '{DefaultPath}'";
        else
            ResolvedResetTooltip = "Reset to default";

        if (!string.IsNullOrWhiteSpace(Watermark))
            ResolvedWatermark = Watermark;
        else
            ResolvedWatermark = "Enter path or leave empty for default";

        CanResetToDefault = CanReset && (Path != DefaultPath);
    }
}

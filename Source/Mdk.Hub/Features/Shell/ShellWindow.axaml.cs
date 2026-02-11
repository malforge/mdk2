using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using JetBrains.Annotations;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Shell;

/// <summary>
/// The main application shell window.
/// </summary>
[Singleton]
[UsedImplicitly]
public partial class ShellWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ShellWindow"/> class.
    /// </summary>
    public ShellWindow()
    {
        InitializeComponent();
        
        // Subscribe to window state and focus changes
        PropertyChanged += OnWindowPropertyChanged;
    }
    
    void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // Update background state when window state or focus changes
        if (e.Property.Name == nameof(WindowState) || e.Property.Name == nameof(IsActive))
        {
            if (DataContext is ShellViewModel viewModel)
                viewModel.UpdateWindowState(WindowState == WindowState.Minimized, IsActive);
        }
    }

    /// <summary>
    /// Called when the window receives focus.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);

        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.WindowFocusWasGained();
            viewModel.UpdateWindowState(WindowState == WindowState.Minimized, IsActive);
        }
    }
    
    /// <summary>
    /// Called when the window loses focus.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        
        if (DataContext is ShellViewModel viewModel)
            viewModel.UpdateWindowState(WindowState == WindowState.Minimized, IsActive);
    }

    /// <summary>
    /// Called when the data context of the window changes.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ShellViewModel viewModel)
        {
            // Subscribe to close requests
            viewModel.CloseRequested += OnCloseRequested;

            // Subscribe to bring-to-front requests
            viewModel.BringToFrontRequested += OnBringToFrontRequested;

            // Restore window settings if they exist
            try
            {
                if (viewModel.Settings.TryGetValue("MainWindowSettings", out WindowSettings windowSettings) && !windowSettings.IsEmpty())
                {
                    windowSettings.Restore(this, viewModel.InitialWindowState);
                }
                // else: Let OS/Avalonia use default size and placement
                
                // Override window state if launched with notification arguments (if Restore didn't already handle it)
                if (viewModel.InitialWindowState.HasValue && WindowState != viewModel.InitialWindowState.Value)
                    WindowState = viewModel.InitialWindowState.Value;
            }
            catch
            {
                // If settings are corrupt, just use OS defaults
            }
        }
    }

    void OnCloseRequested(object? sender, EventArgs e) => Close();

    void OnBringToFrontRequested(object? sender, EventArgs e)
    {
        // Restore if minimized
        if (WindowState == WindowState.Minimized)
            WindowState = WindowState.Normal;

        // Activate and bring to front
        Activate();

        // On Linux (and macOS), also use Topmost trick to ensure window comes to front
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS() || App.IsLinux)
        {
            Topmost = true;
            Topmost = false;
        }
    }

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+R to refresh
        if (e is { Key: Key.R, KeyModifiers: KeyModifiers.Control })
        {
            if (DataContext is ShellViewModel viewModel)
                viewModel.RequestRefresh();
            e.Handled = true;
        }
    }

    /// <summary>
    /// Called when the window is closing.
    /// </summary>
    /// <param name="e">The event arguments.</param>
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel && !e.IsProgrammatic)
        {
            // Cancel the close and check with ViewModel
            e.Cancel = true;

            _ = CheckCanCloseAsync(viewModel);
            return;
        }

        // Save window settings
        if (DataContext is ShellViewModel vm)
        {
            try
            {
                var settings = vm.Settings;
                
                // If closing in Normal state, capture everything
                if (WindowState == WindowState.Normal)
                {
                    var windowSettings = new WindowSettings(this);
                    settings.SetValue("MainWindowSettings", windowSettings);
                }
                // If closing maximized/minimized, preserve existing position/size but update IsMaximized
                else
                {
                    // Try to get existing settings to preserve normal state bounds
                    WindowSettings updatedSettings;
                    if (settings.TryGetValue<WindowSettings>("MainWindowSettings", out var existingSettings))
                    {
                        // Preserve existing position/size, only update IsMaximized
                        updatedSettings = new WindowSettings(
                            existingSettings.Width,
                            existingSettings.Height,
                            existingSettings.Top,
                            existingSettings.Left,
                            isMaximized: WindowState == WindowState.Maximized,
                            existingSettings.LeftPanelWidth);
                    }
                    else
                    {
                        // No existing settings, just save maximized state
                        updatedSettings = new WindowSettings(this);
                    }
                    
                    if (!updatedSettings.IsEmpty())
                        settings.SetValue("MainWindowSettings", updatedSettings);
                }
            }
            catch
            {
                // If we can't save settings, don't crash on close
            }
        }

        base.OnClosing(e);
    }

    async Task CheckCanCloseAsync(ShellViewModel viewModel)
    {
        if (await viewModel.CanCloseAsync())
        {
            // Close programmatically to bypass the check
            Close();
        }
    }

    /// <summary>
    /// Stores window position, size, and state for persistence.
    /// Position and size are nullable - only stored when window is in Normal state.
    /// </summary>
    readonly struct WindowSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSettings"/> struct from an existing window.
        /// Only captures position/size if window is in Normal state.
        /// When maximized/minimized, we cannot reliably get restore bounds, so position/size are null.
        /// </summary>
        /// <param name="window">The window to capture settings from.</param>
        public WindowSettings(Window window)
        {
            // Only save position/size if window is in Normal state
            // Avalonia doesn't expose RestoreBounds, so we can't capture "restore position" when maximized
            if (window.WindowState == WindowState.Normal)
            {
                Width = window.Width;
                Height = window.Height;
                Top = window.Position.Y;
                Left = window.Position.X;
            }
            else
            {
                // When maximized/minimized, Width/Height would be screen dimensions (wrong!)
                // We need to preserve existing saved values instead (handled by caller)
                Width = null;
                Height = null;
                Top = null;
                Left = null;
            }

            IsMaximized = window.WindowState == WindowState.Maximized;

            if (window is ShellWindow shellWindow)
            {
                var mainGrid = shellWindow.FindControl<Grid>("MainContentGrid");
                if (mainGrid?.ColumnDefinitions.Count > 0)
                    LeftPanelWidth = mainGrid.ColumnDefinitions[0].ActualWidth;
                else
                    LeftPanelWidth = null;
            }
            else
                LeftPanelWidth = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowSettings"/> struct from individual values.
        /// </summary>
        /// <param name="width">The window width.</param>
        /// <param name="height">The window height.</param>
        /// <param name="top">The window top position.</param>
        /// <param name="left">The window left position.</param>
        /// <param name="isMaximized">Whether the window was maximized.</param>
        /// <param name="leftPanelWidth">The left panel width.</param>
        [JsonConstructor]
        public WindowSettings(double? width, double? height, double? top, double? left, bool isMaximized, double? leftPanelWidth)
        {
            Width = width;
            Height = height;
            Top = top;
            Left = left;
            IsMaximized = isMaximized;
            LeftPanelWidth = leftPanelWidth;
        }

        /// <summary>
        /// Gets the window width (null if never saved in Normal state).
        /// </summary>
        public double? Width { get; }

        /// <summary>
        /// Gets the window height (null if never saved in Normal state).
        /// </summary>
        public double? Height { get; }

        /// <summary>
        /// Gets the window top position (null if never saved in Normal state).
        /// </summary>
        public double? Top { get; }

        /// <summary>
        /// Gets the window left position (null if never saved in Normal state).
        /// </summary>
        public double? Left { get; }

        /// <summary>
        /// Gets whether the window was maximized.
        /// </summary>
        public bool IsMaximized { get; }

        /// <summary>
        /// Gets the left panel width (null if never saved).
        /// </summary>
        public double? LeftPanelWidth { get; }

        /// <summary>
        /// Determines whether settings have meaningful data to restore.
        /// </summary>
        /// <returns>True if there are no values to restore; otherwise, false.</returns>
        public bool IsEmpty() => !Width.HasValue && !Height.HasValue && !IsMaximized;

        /// <summary>
        /// Restores the window state from these settings.
        /// Only applies values that are present (not null).
        /// </summary>
        /// <param name="window">The window to restore.</param>
        /// <param name="overrideWindowState">Optional window state that takes precedence over saved state.</param>
        public void Restore(Window window, WindowState? overrideWindowState = null)
        {
            // Restore position and size if available
            if (Width.HasValue && Height.HasValue && Left.HasValue && Top.HasValue)
            {
                // Make sure window hasn't gone completely off-screen
                var screen = window.Screens.ScreenFromPoint(new PixelPoint((int)Left.Value, (int)Top.Value));
                double left = Left.Value, top = Top.Value;
                if (screen != null)
                {
                    var workingArea = screen.WorkingArea;
                    if (left < workingArea.X)
                        left = workingArea.X;
                    else if (left > workingArea.Right - 100)
                        left = workingArea.Right - 100;
                    if (top < workingArea.Y)
                        top = workingArea.Y;
                    else if (top > workingArea.Bottom - 100)
                        top = workingArea.Bottom - 100;
                }

                window.Width = Width.Value;
                window.Height = Height.Value;
                window.Position = new PixelPoint((int)left, (int)top);
            }

            // Always restore window state (maximized or normal), unless overridden
            // But don't override if window is already Minimized (set by App.axaml.cs for notification startup)
            if (window.WindowState != WindowState.Minimized)
                window.WindowState = overrideWindowState ?? (IsMaximized ? WindowState.Maximized : WindowState.Normal);

            // Restore panel width if available
            if (LeftPanelWidth.HasValue && window is ShellWindow shellWindow)
            {
                var mainGrid = shellWindow.FindControl<Grid>("MainContentGrid");
                if (mainGrid?.ColumnDefinitions.Count > 0 && Width.HasValue)
                {
                    const double minLeftWidth = 400;
                    const double minRightWidth = 400;
                    const double splitterWidth = 4;

                    var maxLeftWidth = Width.Value - splitterWidth - minRightWidth;
                    var clampedWidth = Math.Max(minLeftWidth, Math.Min(LeftPanelWidth.Value, maxLeftWidth));

                    mainGrid.ColumnDefinitions[0].Width = new GridLength(clampedWidth, GridUnitType.Pixel);
                }
            }
        }
    }
}

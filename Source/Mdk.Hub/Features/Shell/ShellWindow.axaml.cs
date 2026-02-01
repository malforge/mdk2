using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using JetBrains.Annotations;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Shell;

[Singleton]
[UsedImplicitly]
public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);

        if (DataContext is ShellViewModel viewModel)
            viewModel.WindowFocusWasGained();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is ShellViewModel viewModel)
        {
            // Subscribe to close requests
            viewModel.CloseRequested += OnCloseRequested;

            // Restore window settings
            try
            {
                var windowSettings = viewModel.Settings.GetValue("MainWindowSettings", new WindowSettings(this));
                if (!windowSettings.IsNew())
                    windowSettings.Restore(this);
                else
                    CenterWindow(); // Center on first run
            }
            catch
            {
                // If settings are corrupt, just use defaults and center
                CenterWindow();
            }
        }
    }

    void OnCloseRequested(object? sender, EventArgs e) => Close();

    void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Ctrl+R to refresh
        if (e.Key == Key.R && e.KeyModifiers == KeyModifiers.Control)
        {
            if (DataContext is ShellViewModel viewModel)
                viewModel.RequestRefresh();
            e.Handled = true;
        }
    }

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
                var windowSettings = new WindowSettings(this);
                settings.SetValue("MainWindowSettings", windowSettings);
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

    void CenterWindow()
    {
        // Center the window on the primary screen
        var screen = Screens.Primary;
        if (screen != null)
        {
            var workingArea = screen.WorkingArea;
            var scaling = screen.Scaling;
            
            // Convert logical size to physical pixels
            var physicalWidth = Width * scaling;
            var physicalHeight = Height * scaling;
            
            var left = workingArea.X + (workingArea.Width - physicalWidth) / 2;
            var top = workingArea.Y + (workingArea.Height - physicalHeight) / 2;
            Position = new PixelPoint((int)left, (int)top);
        }
    }

    struct WindowSettings
    {
        readonly bool _isNew;

        public WindowSettings(Window window)
        {
            Width = window.Width;
            Height = window.Height;
            Top = window.Position.Y;
            Left = window.Position.X;
            WindowState = window.WindowState;

            if (window is ShellWindow shellWindow)
            {
                var mainGrid = shellWindow.FindControl<Grid>("MainContentGrid");
                if (mainGrid?.ColumnDefinitions.Count > 0)
                    LeftPanelWidth = mainGrid.ColumnDefinitions[0].ActualWidth;
                else
                    LeftPanelWidth = Width / 2;
            }
            else
                LeftPanelWidth = Width / 2;

            _isNew = true;
        }

        [JsonConstructor]
        [Obsolete("For serialization only", true)]
        public WindowSettings(double width, double height, double top, double left, WindowState windowState, double leftPanelWidth = 0)
        {
            Width = width;
            Height = height;
            Top = top;
            Left = left;
            WindowState = windowState;
            LeftPanelWidth = leftPanelWidth > 0 ? leftPanelWidth : width / 2;
            _isNew = false;
        }

        public double Width { get; }
        public double Height { get; }
        public double Top { get; }
        public double Left { get; }
        public WindowState WindowState { get; }
        public double LeftPanelWidth { get; }

        public bool IsNew() => _isNew;

        public void Restore(Window window)
        {
            // Restores the window, while also making sure it hasn't gone completely off-screen
            var screen = window.Screens.ScreenFromPoint(new PixelPoint((int)Left, (int)Top));
            double left = Left, top = Top;
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

            window.Width = Width;
            window.Height = Height;
            window.Position = new PixelPoint((int)left, (int)top);
            window.WindowState = WindowState;

            if (window is ShellWindow shellWindow)
            {
                var mainGrid = shellWindow.FindControl<Grid>("MainContentGrid");
                if (mainGrid?.ColumnDefinitions.Count > 0)
                {
                    const double minLeftWidth = 400;
                    const double minRightWidth = 400;
                    const double splitterWidth = 4;

                    var maxLeftWidth = Width - splitterWidth - minRightWidth;
                    var clampedWidth = Math.Max(minLeftWidth, Math.Min(LeftPanelWidth, maxLeftWidth));

                    mainGrid.ColumnDefinitions[0].Width = new GridLength(clampedWidth, GridUnitType.Pixel);
                }
            }
        }
    }
}
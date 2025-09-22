using System;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using JetBrains.Annotations;
using Mal.DependencyInjection;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable MemberCanBePrivate.Local

namespace Mdk.Hub.Features.Shell;

[Dependency]
[UsedImplicitly]
public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            var settings = viewModel.Settings;
            var windowSettings = settings.GetValue("MainWindowSettings", new WindowSettings(this));
            if (!windowSettings.IsNew())
                windowSettings.Restore(this);
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            var settings = viewModel.Settings;
            var windowSettings = new WindowSettings(this);
            settings.SetValue("MainWindowSettings", windowSettings);
        }
        base.OnClosing(e);
    }

    class WindowSettings
    {
        readonly bool _isNew;

        public WindowSettings(Window window)
        {
            Width = window.Width;
            Height = window.Height;
            Top = window.Position.Y;
            Left = window.Position.X;
            WindowState = window.WindowState;
            _isNew = true;
        }

        [JsonConstructor]
        [Obsolete("For serialization only", true)]
        public WindowSettings(double width, double height, double top, double left, WindowState windowState)
        {
            Width = width;
            Height = height;
            Top = top;
            Left = left;
            WindowState = windowState;
            _isNew = false;
        }

        public double Width { get; }
        public double Height { get; }
        public double Top { get; }
        public double Left { get; }
        public WindowState WindowState { get; }

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
        }
    }
}
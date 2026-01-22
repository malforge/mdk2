using System;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Controls;
using JetBrains.Annotations;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable MemberCanBePrivate.Local

namespace Mdk.Hub.Features.Shell;

[Dependency]
[UsedImplicitly]
public partial class ShellWindow : Window
{
    readonly IShell _shell;
    readonly Mdk.Hub.Features.CommonDialogs.ICommonDialogs _commonDialogs;
    readonly IProjectService _projectService;
    
    public ShellWindow(IShell shell, Mdk.Hub.Features.CommonDialogs.ICommonDialogs commonDialogs, IProjectService projectService)
    {
        _shell = shell;
        _commonDialogs = commonDialogs;
        _projectService = projectService;
        InitializeComponent();
        
        // Initialize easter egg
        var easterEgg = this.FindControl<EasterEgg>("EasterEggControl");
        easterEgg?.Initialize(shell);
    }

    protected override void OnGotFocus(global::Avalonia.Input.GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        
        if (_shell is Shell shellService)
            shellService.RaiseWindowFocusGained();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            try
            {
                var settings = viewModel.Settings;
                var windowSettings = settings.GetValue("MainWindowSettings", new WindowSettings(this));
                if (!windowSettings.IsNew())
                    windowSettings.Restore(this);
            }
            catch
            {
                // If settings are corrupt, just use defaults (current window state)
            }
        }

        base.OnDataContextChanged(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Check for unsaved changes before allowing close
        if (_shell.HasUnsavedChanges() && !e.IsProgrammatic)
        {
            // Cancel the close
            e.Cancel = true;
            
            // Show warning dialog
            _ = WarnAboutUnsavedChangesAsync();
            return;
        }
        
        if (DataContext is ShellViewModel viewModel)
        {
            try
            {
                var settings = viewModel.Settings;
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
    
    async System.Threading.Tasks.Task WarnAboutUnsavedChangesAsync()
    {
        var result = await _commonDialogs.ShowAsync(new Mdk.Hub.Features.CommonDialogs.ConfirmationMessage
        {
            Title = "Unsaved Changes",
            Message = "You have unsaved project configuration changes. Are you sure you want to exit?",
            OkText = "Exit Anyway",
            CancelText = "Show Me"
        });
        
        if (result)
        {
            // Exit Anyway - Close programmatically to bypass the check
            Close();
        }
        else
        {
            // Show Me - Navigate to first project with changes
            var projectPath = _shell.GetFirstProjectWithUnsavedChanges();
            if (projectPath != null && DataContext is ShellViewModel viewModel)
            {
                // Find the project in the overview
                if (viewModel.NavigationView is Mdk.Hub.Features.Projects.Overview.ProjectOverviewViewModel overviewVm)
                {
                    var projectModel = overviewVm.Projects
                        .OfType<Mdk.Hub.Features.Projects.Overview.ProjectModel>()
                        .FirstOrDefault(p => p.ProjectPath == projectPath);
                    
                    if (projectModel != null)
                    {
                        projectModel.SelectCommand.Execute(projectModel);
                    }
                }
            }
        }
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
            
            if (window is ShellWindow shellWindow)
            {
                var mainGrid = shellWindow.FindControl<Grid>("MainContentGrid");
                if (mainGrid?.ColumnDefinitions.Count > 0)
                    LeftPanelWidth = mainGrid.ColumnDefinitions[0].ActualWidth;
                else
                    LeftPanelWidth = Width / 2;
            }
            else
            {
                LeftPanelWidth = Width / 2;
            }
            
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
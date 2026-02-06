using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Projects.Actions;

[Singleton]
public partial class ProjectActionsView : UserControl
{
    ProjectActionsViewModel? _currentViewModel;

    public ProjectActionsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    void OnLoaded(object? sender, RoutedEventArgs e) =>
        // Initialize drawer state on load
        AnimateDrawer();

    void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from old view model
        if (_currentViewModel != null)
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        // Subscribe to new view model
        if (DataContext is ProjectActionsViewModel vm)
        {
            _currentViewModel = vm;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProjectActionsViewModel.IsOptionsDrawerOpen))
            AnimateDrawer();
    }

    async void AnimateDrawer()
    {
        if (DataContext is not ProjectActionsViewModel vm) return;

        var drawer = this.FindControl<Border>("OptionsDrawer");
        if (drawer?.RenderTransform is not TranslateTransform transform) return;

        if (vm.IsOptionsDrawerOpen)
        {
            // Show drawer and animate in
            drawer.IsVisible = true;
            await Task.Delay(1); // Let IsVisible take effect

            // Animate from current position to 0
            var startX = transform.X;
            var endX = 0.0;
            var duration = 250;
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < duration)
            {
                var progress = (DateTime.Now - startTime).TotalMilliseconds / duration;
                // Cubic ease out
                progress = 1 - Math.Pow(1 - progress, 3);
                transform.X = startX + (endX - startX) * progress;
                await Task.Delay(16); // ~60fps
            }
            transform.X = endX;
        }
        else
        {
            // Animate out then hide
            var startX = transform.X;
            var endX = 1000.0;
            var duration = 250;
            var startTime = DateTime.Now;

            while ((DateTime.Now - startTime).TotalMilliseconds < duration)
            {
                var progress = (DateTime.Now - startTime).TotalMilliseconds / duration;
                // Cubic ease out
                progress = 1 - Math.Pow(1 - progress, 3);
                transform.X = startX + (endX - startX) * progress;
                await Task.Delay(16); // ~60fps
            }
            transform.X = endX;
            drawer.IsVisible = false;
        }
    }

    void OnCloseDrawerClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ProjectActionsViewModel vm)
            vm.CloseOptionsDrawer();
    }
}

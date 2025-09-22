using Mal.DependencyInjection;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Features.Settings;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Shell;

[Dependency]
[ViewModelFor<ShellWindow>]
public class ShellViewModel : ViewModel
{
    ViewModel? _currentView;
    ViewModel? _navigationView;

    public ShellViewModel() : this(null!, null!)
    {
        IsDesignMode = true;
    }

    public ShellViewModel(ISettings settings, ProjectOverviewViewModel projectOverviewViewModel)
    {
        Settings = settings;
        CurrentView = projectOverviewViewModel;
    }

    public bool IsDesignMode { get; }

    public ISettings Settings { get; }

    public ViewModel? CurrentView
    {
        get => _currentView;
        private set => SetProperty(ref _currentView, value);
    }

    public ViewModel? NavigationView
    {
        get => _navigationView;
        private set => SetProperty(ref _navigationView, value);
    }
}
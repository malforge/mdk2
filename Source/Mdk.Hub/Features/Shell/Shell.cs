using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Settings;

namespace Mdk.Hub.Features.Shell;

[Dependency<IShell>]
public class Shell(IDependencyContainer container, Lazy<ShellViewModel> lazyViewModel, ISettings settings) : IShell
{
    readonly IDependencyContainer _container = container;
    readonly Lazy<ShellViewModel> _viewModel = lazyViewModel;
    readonly ISettings _settings = settings;
    readonly System.Collections.Generic.HashSet<string> _projectsWithUnsavedChanges = new();
    DateTime? _easterEggDisabledUntil;
    bool _easterEggDisabledForever;

    public event EventHandler? WindowFocusGained;
    public event EventHandler? EasterEggActiveChanged;

    public ObservableCollection<ToastMessage> ToastMessages { get; } = new();

    public bool IsEasterEggActive
    {
        get
        {
#if DEBUG_EASTER_EGG
            var isEasterEggDay = true;
#else
            var today = DateTime.Now;
            var isEasterEggDay = today is { Month: 11, Day: 23 };
#endif
            var isDisabled = _easterEggDisabledForever || 
                           (_easterEggDisabledUntil.HasValue && DateTime.Now < _easterEggDisabledUntil.Value);
            
            return isEasterEggDay && !isDisabled;
        }
    }

    public void Start()
    {
        // Load easter egg settings
        _easterEggDisabledForever = _settings.GetValue("EasterEggDisabledForever", false);
        var disabledUntilTicks = _settings.GetValue("EasterEggDisabledUntilTicks", 0L);
        if (disabledUntilTicks > 0)
            _easterEggDisabledUntil = new DateTime(disabledUntilTicks);
    }

    public void AddOverlay(OverlayModel model)
    {
        void onDismissed(object? sender, EventArgs e)
        {
            model.Dismissed -= onDismissed;
            _viewModel.Value.OverlayViews.Remove(model);
            if (model is IDisposable disposable) disposable.Dispose();
        }

        model.Dismissed += onDismissed;
        _viewModel.Value.OverlayViews.Add(model);
    }

    public void ShowToast(string message, int durationMs = 3000)
    {
        var toast = new ToastMessage { Message = message };
        ToastMessages.Add(toast);
        
        // Start dismiss animation before removal
        Task.Delay(durationMs - 200).ContinueWith(_ =>
        {
            toast.IsDismissing = true;
        }, TaskScheduler.FromCurrentSynchronizationContext());
        
        // Remove after fade-out animation completes
        Task.Delay(durationMs).ContinueWith(_ =>
        {
            ToastMessages.Remove(toast);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void RaiseWindowFocusGained()
    {
        WindowFocusGained?.Invoke(this, EventArgs.Empty);
    }

    public void DisableEasterEggForToday()
    {
        _easterEggDisabledUntil = DateTime.Now.Date.AddDays(1);
        _settings.SetValue("EasterEggDisabledUntilTicks", _easterEggDisabledUntil.Value.Ticks);
        EasterEggActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public void DisableEasterEggForever()
    {
        _easterEggDisabledForever = true;
        _settings.SetValue("EasterEggDisabledForever", true);
        EasterEggActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public bool HasUnsavedChanges()
    {
        return _projectsWithUnsavedChanges.Count > 0;
    }

    public string? GetFirstProjectWithUnsavedChanges()
    {
        return _projectsWithUnsavedChanges.FirstOrDefault();
    }

    public void SetProjectUnsavedState(string projectPath, bool hasUnsavedChanges)
    {
        if (hasUnsavedChanges)
            _projectsWithUnsavedChanges.Add(projectPath);
        else
            _projectsWithUnsavedChanges.Remove(projectPath);
    }
}
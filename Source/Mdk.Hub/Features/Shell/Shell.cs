using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Projects.NewProjectDialog;
using Mdk.Hub.Features.Settings;

namespace Mdk.Hub.Features.Shell;

[Singleton<IShell>]
public class Shell(IDependencyContainer container, Lazy<ShellViewModel> lazyViewModel, ISettings settings) : IShell
{
    readonly IDependencyContainer _container = container;
    readonly ISettings _settings = settings;
    readonly List<Action<string[]>> _startupCallbacks = new();
    readonly List<UnsavedChangesRegistration> _unsavedChangesRegistrations = new();
    readonly Lazy<ShellViewModel> _viewModel = lazyViewModel;
    bool _hasStarted;
    string[]? _startupArgs;

    public event EventHandler? WindowFocusGained;
    public event EventHandler? RefreshRequested;

    public ObservableCollection<ToastMessage> ToastMessages { get; } = new();

    public void Start(string[] args)
    {
        _startupArgs = args;
        _hasStarted = true;

        // Invoke queued callbacks
        foreach (var callback in _startupCallbacks)
            callback(args);
        _startupCallbacks.Clear();
    }

    public void WhenStarted(Action<string[]> callback)
    {
        if (_hasStarted)
            callback(_startupArgs!);
        else
            _startupCallbacks.Add(callback);
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
        Task.Delay(durationMs - 200).ContinueWith(_ => { toast.IsDismissing = true; }, TaskScheduler.FromCurrentSynchronizationContext());

        // Remove after fade-out animation completes
        Task.Delay(durationMs).ContinueWith(_ => { ToastMessages.Remove(toast); }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public UnsavedChangesHandle RegisterUnsavedChanges(string description, Action navigateToChanges)
    {
        var registration = new UnsavedChangesRegistration
        {
            Description = description,
            NavigateAction = navigateToChanges
        };

        _unsavedChangesRegistrations.Add(registration);

        return new UnsavedChangesHandle(() => _unsavedChangesRegistrations.Remove(registration));
    }

    public bool TryGetUnsavedChangesInfo(out UnsavedChangesInfo info)
    {
        if (_unsavedChangesRegistrations.Count == 0)
        {
            info = default;
            return false;
        }

        if (_unsavedChangesRegistrations.Count == 1)
        {
            // Single registration: use its description and action
            var registration = _unsavedChangesRegistrations[0];
            info = new UnsavedChangesInfo
            {
                Description = registration.Description,
                GoThereAction = registration.NavigateAction
            };
        }
        else
        {
            // Multiple registrations: generic message with no-op action
            info = new UnsavedChangesInfo
            {
                Description = "You have unsaved changes.",
                GoThereAction = () => { }
            };
        }

        return true;
    }

    public void RaiseWindowFocusGained() => WindowFocusGained?.Invoke(this, EventArgs.Empty);

    public void RequestRefresh()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        ShowToast("Refreshing...", 1500);
    }

    // Dialog methods
    public Task ShowOverlayAsync(OverlayModel model)
    {
        var tcs = new TaskCompletionSource();

        void handler(object? sender, EventArgs e)
        {
            model.Dismissed -= handler;
            tcs.SetResult();
        }

        model.Dismissed += handler;
        AddOverlay(model);
        return tcs.Task;
    }

    public async Task<bool> ShowAsync(ConfirmationMessage message)
    {
        var model = new MessageBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true
                },
                new MessageBoxChoice
                {
                    Text = message.CancelText,
                    Value = false
                }
            ]
        };

        await ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    public async Task ShowAsync(InformationMessage message)
    {
        var model = new MessageBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true,
                    IsDefault = true
                }
            ]
        };

        await ShowOverlayAsync(model);
    }

    public async Task<bool> ShowAsync(KeyPhraseValidationMessage message)
    {
        var model = new DangerBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            RequiredKeyPhrase = message.RequiredKeyPhrase,
            KeyPhraseWatermark = message.KeyPhraseWatermark,
            Choices =
            [
                new DangerousMessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true,
                    IsDefault = true
                },
                new MessageBoxChoice
                {
                    Text = message.CancelText,
                    Value = false,
                    IsCancel = true
                }
            ]
        };

        await ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    public async Task ShowBusyOverlayAsync(BusyOverlayViewModel busyOverlay) =>
        await ShowOverlayAsync(busyOverlay);

    class UnsavedChangesRegistration
    {
        public string Description { get; init; } = string.Empty;
        public Action NavigateAction { get; init; } = () => { };
    }
}
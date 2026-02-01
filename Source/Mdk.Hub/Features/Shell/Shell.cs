using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.NewProjectDialog;
using Mdk.Hub.Features.Settings;

namespace Mdk.Hub.Features.Shell;

[Singleton<IShell>]
public class Shell(IDependencyContainer container, ISettings settings, ILogger logger) : IShell
{
    readonly IDependencyContainer _container = container;
    readonly ISettings _settings = settings;
    readonly ILogger _logger = logger;
    readonly List<Action<string[]>> _startupCallbacks = new();
    readonly List<Action<string[]>> _readyCallbacks = new();
    readonly List<UnsavedChangesRegistration> _unsavedChangesRegistrations = new();
    bool _hasStarted;
    bool _isReady;
    string[]? _startupArgs;

    public event EventHandler? WindowFocusGained;
    public event EventHandler? RefreshRequested;

    public ObservableCollection<ToastMessage> ToastMessages { get; } = new();
    public ObservableCollection<OverlayModel> OverlayViews { get; } = new();

    public void Start(string[] args)
    {
        _startupArgs = args;
        _hasStarted = true;

        // Write Hub executable path for MDK CLI to discover
        WriteHubPath();
        
        // Invoke queued callbacks
        foreach (var callback in _startupCallbacks)
            callback(args);
        _startupCallbacks.Clear();
        
        // On Linux, check if required paths are configured AFTER startup completes
        if (App.IsLinux)
        {
            Task.Delay(500).ContinueWith(_ =>
            {
                CheckLinuxPathConfiguration();
            });
        }
        
        // If not on Linux or paths are valid, we're ready immediately
        if (!App.IsLinux || !RequiresLinuxPathConfiguration())
            SetReady();
    }

    public void WhenStarted(Action<string[]> callback)
    {
        if (_hasStarted)
            callback(_startupArgs!);
        else
            _startupCallbacks.Add(callback);
    }
    
    public void WhenReady(Action<string[]> callback)
    {
        if (_isReady)
            callback(_startupArgs!);
        else
            _readyCallbacks.Add(callback);
    }
    
    void SetReady()
    {
        if (_isReady)
            return;
            
        _isReady = true;
        _logger.Info("Shell is ready for operation");
        
        foreach (var callback in _readyCallbacks)
            callback(_startupArgs!);
        _readyCallbacks.Clear();
    }
    
    bool RequiresLinuxPathConfiguration()
    {
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var binPath = hubSettings.CustomAutoBinaryPath;
        var scriptOutputPath = hubSettings.CustomAutoScriptOutputPath;
        var modOutputPath = hubSettings.CustomAutoModOutputPath;
        
        return binPath == "auto" || scriptOutputPath == "auto" || modOutputPath == "auto";
    }

    public void AddOverlay(OverlayModel model)
    {
        void onDismissed(object? sender, EventArgs e)
        {
            model.Dismissed -= onDismissed;
            OverlayViews.Remove(model);
            if (model is IDisposable disposable) disposable.Dispose();
        }

        model.Dismissed += onDismissed;
        OverlayViews.Add(model);
    }
    
    void ShowGlobalOptions(bool openedForLinuxValidation = false)
    {
        var viewModel = App.Container.Resolve<GlobalSettingsViewModel>();
        if (openedForLinuxValidation)
        {
            viewModel.MarkAsOpenedForLinuxValidation();
            
            // When dismissed, check if we can now set ready
            viewModel.Dismissed += (_, _) =>
            {
                // If paths are now valid, we're ready
                if (!RequiresLinuxPathConfiguration())
                    SetReady();
            };
        }
        AddOverlay(viewModel);
    }

    public void Shutdown()
    {
        _logger.Info("Shutdown requested");
        System.Environment.Exit(0);
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

    void WriteHubPath()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (exePath == null)
            {
                _logger.Warning("Could not determine Hub executable path");
                return;
            }

            // Resolve to actual path if it's a symlink (Velopack 'current' directory)
            var resolvedPath = ResolveSymlink(exePath);

            // Write to %AppData%/MDK2/Hub/hub.path or ~/.config/MDK2/Hub/hub.path
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var hubFolder = Path.Combine(appDataFolder, "MDK2", "Hub");
            Directory.CreateDirectory(hubFolder);

            var pathFile = Path.Combine(hubFolder, "hub.path");
            File.WriteAllText(pathFile, resolvedPath);

            _logger.Info($"Hub path written to: {pathFile}");
            _logger.Debug($"Hub executable: {resolvedPath}");
        }
        catch (Exception ex)
        {
            _logger.Warning($"Failed to write Hub path file: {ex.Message}");
        }
    }
    
    void CheckLinuxPathConfiguration()
    {
        if (!App.IsLinux)
            return;
            
        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        var binPath = hubSettings.CustomAutoBinaryPath;
        var scriptOutputPath = hubSettings.CustomAutoScriptOutputPath;
        var modOutputPath = hubSettings.CustomAutoModOutputPath;
        
        // On Linux, "auto" is not acceptable - paths must be explicitly set
        if (binPath == "auto" || scriptOutputPath == "auto" || modOutputPath == "auto")
        {
            _logger.Info($"Linux: Required paths not configured (bin={binPath}, script={scriptOutputPath}, mod={modOutputPath}), opening global options");
            
            ShowGlobalOptions(openedForLinuxValidation: true);
        }
    }

    static string ResolveSymlink(string path)
    {
        try
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.LinkTarget != null)
            {
                var targetPath = Path.GetFullPath(fileInfo.LinkTarget, Path.GetDirectoryName(path) ?? string.Empty);
                return File.Exists(targetPath) ? targetPath : path;
            }

            return path;
        }
        catch
        {
            return path;
        }
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
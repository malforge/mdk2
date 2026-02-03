using System;
using System.Diagnostics;
using System.Windows.Input;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Announcements;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Projects.Actions;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions.Items;

[Singleton]
[ViewModelFor<AnnouncementsActionView>]
public class AnnouncementsAction : ActionItem
{
    readonly IAnnouncementService _announcementService;
    readonly ILogger _logger;
    readonly IShell _shell;
    Announcement? _currentAnnouncement;

    public AnnouncementsAction(IShell shell, IAnnouncementService announcementService, ILogger logger)
    {
        _shell = shell;
        _announcementService = announcementService;
        _logger = logger;

        _logger.Info("AnnouncementsAction created");

        DismissCommand = new RelayCommand(Dismiss);
        OpenLinkCommand = new RelayCommand(OpenLink, () => CurrentAnnouncement?.Link != null);

        // Subscribe to announcement updates
        _logger.Info("Subscribing to announcement updates");
        _announcementService.WhenAnnouncementAvailable(OnAnnouncementAvailable);
        _announcementService.AnnouncementChanged += OnAnnouncementChanged;

        // Subscribe to refresh requests
        _shell.RefreshRequested += OnRefreshRequested;

        // Check if announcement is already loaded
        if (_announcementService.LastKnownAnnouncement != null && 
            !_announcementService.IsAnnouncementDismissed(_announcementService.LastKnownAnnouncement.Id))
        {
            _logger.Info($"Setting initial announcement: {_announcementService.LastKnownAnnouncement.Id}");
            CurrentAnnouncement = _announcementService.LastKnownAnnouncement;
        }
        else
        {
            _logger.Info("No announcement available at construction time");
        }
    }

    public override string? Category => null; // No category - appears at top
    public override bool IsGlobal => true; // Global action, not project-specific

    public override bool ShouldShow() => CurrentAnnouncement != null;

    public Announcement? CurrentAnnouncement
    {
        get => _currentAnnouncement;
        set
        {
            if (SetProperty(ref _currentAnnouncement, value))
            {
                _logger.Info($"CurrentAnnouncement set to: {value?.Id ?? "null"}");
                RaiseShouldShowChanged();
                ((RelayCommand)OpenLinkCommand).NotifyCanExecuteChanged();
            }
        }
    }

    public ICommand DismissCommand { get; }
    public ICommand OpenLinkCommand { get; }

    void OnAnnouncementAvailable(Announcement announcement)
    {
        _logger.Info($"OnAnnouncementAvailable called with: {announcement.Id}");
        CurrentAnnouncement = announcement;
    }

    void OnAnnouncementChanged(object? sender, Announcement announcement)
    {
        _logger.Info($"OnAnnouncementChanged called with: {announcement.Id}");
        CurrentAnnouncement = announcement;
    }

    void OnRefreshRequested(object? sender, EventArgs e)
    {
        _logger.Info("Refresh requested, forcing announcement check");
        // Force a fresh announcement check
        _ = _announcementService.ForceCheckAsync();
    }

    void Dismiss()
    {
        if (CurrentAnnouncement != null)
        {
            _announcementService.DismissAnnouncement(CurrentAnnouncement.Id);
            CurrentAnnouncement = null;
        }
    }

    void OpenLink()
    {
        if (CurrentAnnouncement?.Link == null)
            return;

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = CurrentAnnouncement.Link,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _shell.ShowToast($"Failed to open link: {ex.Message}");
        }
    }
}


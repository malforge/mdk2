using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Settings;

namespace Mdk.Hub.Features.Announcements;

[Singleton<IAnnouncementService>]
public class AnnouncementService : IAnnouncementService
{
    const string RemoteUrl = $"{EnvironmentMetadata.GitHubRawContentBaseUrl}/announcements.json";
    static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(6);
    static readonly TimeSpan AutoCheckInterval = TimeSpan.FromHours(1);
    readonly Timer _autoCheckTimer;
    readonly List<Action<Announcement>> _callbacks = new();
    readonly ISettings _settings;
    readonly ILogger _logger;
    int _isChecking; // 0 = not checking, 1 = checking

    public AnnouncementService(ILogger logger, ISettings settings)
    {
        _logger = logger;
        _settings = settings;

        // Start auto-check timer (runs every hour)
        _autoCheckTimer = new Timer(
            _ => _ = CheckForAnnouncementsAsync(),
            null,
            AutoCheckInterval,
            AutoCheckInterval);
    }

    public Announcement? LastKnownAnnouncement { get; private set; }

    public event EventHandler<Announcement>? AnnouncementChanged;

    public void WhenAnnouncementAvailable(Action<Announcement> callback)
    {
        _logger.Info($"WhenAnnouncementAvailable called. LastKnownAnnouncement: {(LastKnownAnnouncement != null ? LastKnownAnnouncement.Id : "null")}");
        if (LastKnownAnnouncement != null && !IsAnnouncementDismissed(LastKnownAnnouncement.Id))
        {
            _logger.Info($"Invoking callback immediately with announcement: {LastKnownAnnouncement.Id}");
            callback(LastKnownAnnouncement);
        }
        else
        {
            _logger.Info("Adding callback to pending list");
            _callbacks.Add(callback);
        }
    }

    public async Task<bool> CheckForAnnouncementsAsync()
    {
        // Check cache expiry
        if (LastKnownAnnouncement != null && DateTime.UtcNow - LastKnownAnnouncement.FetchedAt < CacheExpiry)
        {
            _logger.Info("Announcement check skipped - cache still valid");
            return true;
        }

        return await PerformCheckAsync();
    }

    public async Task<bool> ForceCheckAsync()
    {
        _logger.Info("Force checking for announcements");
        return await PerformCheckAsync();
    }

    public void DismissAnnouncement(string announcementId)
    {
        if (string.IsNullOrEmpty(announcementId))
            return;

        var hubSettings = _settings.GetValue(SettingsKeys.HubSettings, new HubSettings());
        if (!hubSettings.DismissedAnnouncementIds.Contains(announcementId))
        {
            hubSettings.DismissedAnnouncementIds = hubSettings.DismissedAnnouncementIds.Add(announcementId);
            _settings.SetValue(SettingsKeys.HubSettings, hubSettings);
            _logger.Info($"Dismissed announcement: {announcementId}");
        }
    }

    public bool IsAnnouncementDismissed(string announcementId) => _settings.GetValue(SettingsKeys.HubSettings, new HubSettings()).DismissedAnnouncementIds.Contains(announcementId);

    async Task<bool> PerformCheckAsync()
    {
        // Reentry guard
        if (Interlocked.CompareExchange(ref _isChecking, 1, 0) == 1)
        {
            _logger.Info("Announcement check already in progress, skipping");
            return false;
        }

        try
        {
            _logger.Info("═══ Checking for announcements ═══");

            // Try local file first (for testing)
            var localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MDK2", "Hub", "announcements.json");
            Announcement? announcement = null;

            if (File.Exists(localPath))
            {
                _logger.Info($"  Checking local file: {localPath}");
                announcement = await LoadFromFileAsync(localPath);
                if (announcement != null)
                    _logger.Info("  → Loaded from local file");
            }

            // Fall back to remote if no local file
            if (announcement == null)
            {
                _logger.Info($"  Checking remote: {RemoteUrl}");
                announcement = await LoadFromUrlAsync(RemoteUrl);
                if (announcement != null)
                    _logger.Info("  → Loaded from remote");
            }

            if (announcement != null)
            {
                var hasChanged = LastKnownAnnouncement?.Id != announcement.Id;
                LastKnownAnnouncement = announcement;

                // Invoke callbacks if announcement isn't dismissed
                if (!IsAnnouncementDismissed(announcement.Id))
                {
                    _logger.Info($"✓ Announcement found: '{announcement.Message.Substring(0, Math.Min(50, announcement.Message.Length))}...' (ID: {announcement.Id}, Style: {announcement.Style})");
                    _logger.Info($"  Invoking {_callbacks.Count} callbacks");
                    foreach (var callback in _callbacks)
                        callback(announcement);
                    _callbacks.Clear();

                    // Raise event if announcement changed
                    if (hasChanged)
                    {
                        _logger.Info("  Raising AnnouncementChanged event");
                        AnnouncementChanged?.Invoke(this, announcement);
                    }
                }
                else
                    _logger.Info($"✓ Announcement found but already dismissed (ID: {announcement.Id})");

                return true;
            }

            _logger.Info("✓ No announcements available");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to check for announcements: {ex.Message}");
            return false;
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    async Task<Announcement?> LoadFromFileAsync(string path)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path);
            var result = ParseAnnouncement(json);
            if (result == null)
                _logger.Info("  → File exists but contains no valid announcement");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"  → Failed to load file: {ex.Message}");
            return null;
        }
    }

    async Task<Announcement?> LoadFromUrlAsync(string url)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var json = await client.GetStringAsync(url);
            var result = ParseAnnouncement(json);
            if (result == null)
                _logger.Info("  → Remote returned no valid announcement");
            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"  → Failed to fetch from URL: {ex.Message}");
            return null;
        }
    }

    Announcement? ParseAnnouncement(string json)
    {
        try
        {
            _logger.Info($"  Attempting to parse JSON (length: {json.Length})");
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            // Try to parse as array first
            try
            {
                var announcements = JsonSerializer.Deserialize<Announcement[]>(json, options);
                if (announcements != null && announcements.Length > 0)
                {
                    _logger.Info($"  Parsed {announcements.Length} announcement(s) from array");
                    return SelectBestAnnouncement(announcements);
                }
            }
            catch
            {
                // Not an array, try single object
            }

            // Try single object
            var announcement = JsonSerializer.Deserialize<Announcement>(json, options);
            _logger.Info($"  Deserialized single announcement: {(announcement == null ? "null" : "not null")}");

            if (announcement != null)
            {
                var hasId = !string.IsNullOrWhiteSpace(announcement.Id);
                var hasMessage = !string.IsNullOrWhiteSpace(announcement.Message);
                _logger.Info($"  Has Id: {hasId}, Has Message: {hasMessage}");

                if (hasId && hasMessage)
                {
                    var withFetch = announcement with { FetchedAt = DateTime.UtcNow };
                    return IsAnnouncementReady(withFetch) ? withFetch : null;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"  → JSON parse error: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }

    Announcement? SelectBestAnnouncement(Announcement[] announcements)
    {
        var now = DateTime.UtcNow;
        var fetchTime = DateTime.UtcNow;

        // Filter to announcements that are ready to show and not dismissed
        var eligible = announcements
            .Where(a => !string.IsNullOrWhiteSpace(a.Id) && !string.IsNullOrWhiteSpace(a.Message))
            .Select(a => a with { FetchedAt = fetchTime })
            .Where(IsAnnouncementReady)
            .Where(a => !IsAnnouncementDismissed(a.Id))
            .OrderByDescending(a => a.ShowAfter ?? DateTime.MinValue) // Most recent showAfter first
            .ToList();

        if (eligible.Count == 0)
        {
            _logger.Info($"  No eligible announcements (filtered from {announcements.Length} total)");
            return null;
        }

        var selected = eligible.First();
        _logger.Info($"  Selected announcement '{selected.Id}' (from {eligible.Count} eligible)");
        return selected;
    }

    bool IsAnnouncementReady(Announcement announcement)
    {
        if (announcement.ShowAfter == null)
            return true;

        var isReady = DateTime.UtcNow >= announcement.ShowAfter.Value;
        if (!isReady)
            _logger.Info($"  Announcement '{announcement.Id}' not ready (shows after {announcement.ShowAfter.Value:u})");

        return isReady;
    }
}

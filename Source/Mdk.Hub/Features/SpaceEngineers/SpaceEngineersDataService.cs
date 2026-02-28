using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.CommonDialogs;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Loads Space Engineers block definition data from SBC files and caches it on disk.
///     On first use the service resolves the SE installation, checks whether the on-disk cache
///     is still current, and rebuilds it (with a busy overlay) when it is not.
///     Concurrent callers all wait on the same semaphore and receive results once loading completes.
/// </summary>
[Singleton<ISpaceEngineersDataService>]
public class SpaceEngineersDataService(IFileStorageService storage, IShell shell, ILogger logger)
    : ISpaceEngineersDataService
{
    static readonly JsonSerializerOptions JsonOptions = new();

    readonly SemaphoreSlim _lock = new(1, 1);
    bool _loaded;
    string? _loadError;
    ApiErrorKind _loadErrorKind;
    Dictionary<BlockId, BlockInfo> _blocks = new();
    List<BlockCategory> _categories = [];
    HashSet<string> _terminalTypeIds = new();

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<BlockCategory>>> GetCategoriesAsync()
    {
        if (!await EnsureLoadedAsync())
            return ApiResult<IReadOnlyList<BlockCategory>>.Fail(_loadError!, _loadErrorKind);
        return ApiResult<IReadOnlyList<BlockCategory>>.Ok(_categories);
    }

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<BlockInfo>>> GetAllBlocksAsync()
    {
        if (!await EnsureLoadedAsync())
            return ApiResult<IReadOnlyList<BlockInfo>>.Fail(_loadError!, _loadErrorKind);
        return ApiResult<IReadOnlyList<BlockInfo>>.Ok(_blocks.Values.ToList());
    }

    /// <inheritdoc />
    public async Task<ApiResult<BlockInfo>> GetBlockAsync(string typeId, string subtypeId)
    {
        if (!await EnsureLoadedAsync())
            return ApiResult<BlockInfo>.Fail(_loadError!, _loadErrorKind);

        var id = new BlockId(typeId, subtypeId);
        return _blocks.TryGetValue(id, out var block)
            ? ApiResult<BlockInfo>.Ok(block)
            : ApiResult<BlockInfo>.Fail($"Block {id} not found.", ApiErrorKind.NotFound);
    }

    async Task<bool> EnsureLoadedAsync()
    {
        if (_loaded || _loadError != null)
            return _loaded;

        await _lock.WaitAsync();
        try
        {
            if (_loaded || _loadError != null)
                return _loaded;
            await LoadAsync();
            return _loaded;
        }
        finally
        {
            _lock.Release();
        }
    }

    async Task LoadAsync()
    {
        var contentPath = ResolveContentPath();
        if (contentPath == null || !Directory.Exists(contentPath))
        {
            _loadError = "Space Engineers installation not found. Please install Space Engineers or configure the binary path in settings.";
            _loadErrorKind = ApiErrorKind.Unavailable;
            logger.Warning(_loadError);
            return;
        }

        try
        {
            var localizationPath = Path.Combine(contentPath, "Data", "Localization", "MyTexts.resx");
            var categoryPath = Path.Combine(contentPath, "Data", "BlockCategories.sbc");
            var cubeBlocksDir = Path.Combine(contentPath, "Data", "CubeBlocks");

            var cacheDir = storage.GetLocalApplicationDataPath("cache");
            var dataPath = Path.Combine(cacheDir, "se-blocks-data.json");
            var metaPath = Path.Combine(cacheDir, "se-blocks-meta.json");

            var sources = SpaceEngineersDataParser.BuildSourceFileList(localizationPath, categoryPath, cubeBlocksDir);

            if (SpaceEngineersDataParser.IsCacheFresh(metaPath, sources))
                await LoadFromCacheAsync(dataPath);
            else
                await RebuildCacheAsync(localizationPath, categoryPath, cubeBlocksDir, cacheDir, dataPath, metaPath, sources);

            var binPath = ResolveBinPath(contentPath);
            await LoadTerminalTypesAsync(binPath, cacheDir);

            if (_terminalTypeIds.Count > 0)
                FilterToTerminalBlocks();

            _loaded = true;
            logger.Info($"SE data ready: {_blocks.Count} blocks, {_categories.Count} categories.");
        }
        catch (Exception ex)
        {
            _loadError = $"Failed to load Space Engineers data: {ex.Message}";
            _loadErrorKind = ApiErrorKind.ParseError;
            logger.Error(_loadError, ex);
        }
    }

    /// <summary>
    ///     Resolves the path to the Space Engineers <c>Content/</c> directory.
    ///     Returns <c>null</c> if SE is not installed or the platform is unsupported.
    /// </summary>
    /// <remarks>
    ///     Protected virtual to allow test subclasses to inject a fixture content path.
    ///     TODO: Must resolve via user settings before release — allow manual binary path override.
    ///     See <c>Mdk.References</c> for the full Steam/registry detection logic.
    /// </remarks>
    protected virtual string? ResolveContentPath()
    {
        if (!OperatingSystem.IsWindows())
            return null;
        try
        {
            return new Mdk.Hub.Utility.SpaceEngineers().GetInstallPath("Content");
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     Resolves the path to the Space Engineers <c>Bin64/</c> directory by going up one level
    ///     from the Content directory.  Returns <c>null</c> if the directory is not found.
    /// </summary>
    /// <remarks>Protected virtual to allow test subclasses to return <c>null</c> (skips terminal scan).</remarks>
    protected virtual string? ResolveBinPath(string contentPath)
    {
        var seRoot = Path.GetDirectoryName(contentPath);
        if (seRoot == null) return null;
        var binPath = Path.Combine(seRoot, "Bin64");
        return Directory.Exists(binPath) ? binPath : null;
    }

    async Task LoadTerminalTypesAsync(string? binPath, string cacheDir)
    {
        if (binPath == null)
            return;

        var terminalDataPath = Path.Combine(cacheDir, "se-terminal-types.json");
        var terminalMetaPath = Path.Combine(cacheDir, "se-terminal-types-meta.json");
        var terminalSources = SpaceEngineersTerminalScanner.GetDllPaths(binPath);

        if (SpaceEngineersDataParser.IsCacheFresh(terminalMetaPath, terminalSources))
        {
            if (File.Exists(terminalDataPath))
            {
                var json = await File.ReadAllTextAsync(terminalDataPath);
                var cache = JsonSerializer.Deserialize<TerminalTypesCacheData>(json);
                if (cache != null)
                    _terminalTypeIds = new HashSet<string>(cache.TypeIds, StringComparer.OrdinalIgnoreCase);
            }
            return;
        }

        var overlay = new BusyOverlayViewModel("Scanning Space Engineers terminal block types...");
        var shellTask = shell.ShowBusyOverlayAsync(overlay);
        try
        {
            await Task.Run(() =>
            {
                var typeIds = SpaceEngineersTerminalScanner.Scan(binPath);

                Directory.CreateDirectory(cacheDir);

                var cacheData = new TerminalTypesCacheData { TypeIds = [.. typeIds] };
                var meta = new BlocksCacheMeta
                {
                    Sources = terminalSources.ToDictionary(
                        s => s,
                        s => File.Exists(s) ? File.GetLastWriteTimeUtc(s).ToString("O") : string.Empty)
                };

                File.WriteAllText(terminalDataPath, JsonSerializer.Serialize(cacheData, JsonOptions));
                File.WriteAllText(terminalMetaPath, JsonSerializer.Serialize(meta, JsonOptions));

                _terminalTypeIds = typeIds;
            });
        }
        finally
        {
            overlay.Dismiss();
            await shellTask;
        }
    }

    void FilterToTerminalBlocks()
    {
        var keysToRemove = _blocks.Keys
            .Where(id => !_terminalTypeIds.Contains(id.TypeId))
            .ToList();
        foreach (var key in keysToRemove)
            _blocks.Remove(key);

        _categories = _categories
            .Select(c => c with { Items = c.Items.Where(id => _terminalTypeIds.Contains(id.TypeId)).ToList() })
            .Where(c => c.Items.Count > 0)
            .ToList();

        logger.Info($"Filtered to terminal blocks: {_blocks.Count} blocks, removed {keysToRemove.Count} non-terminal types.");
    }

    async Task LoadFromCacheAsync(string dataPath)
    {
        var json = await File.ReadAllTextAsync(dataPath);
        var cache = JsonSerializer.Deserialize<BlocksCacheData>(json)
                    ?? throw new InvalidOperationException("Cache data file is empty or corrupt.");

        _blocks = cache.Blocks.ToDictionary(
            b => new BlockId(b.TypeId, b.SubtypeId),
            b => new BlockInfo(new BlockId(b.TypeId, b.SubtypeId), b.DisplayName, b.IconPath, b.CubeSize, b.Dlc));

        _categories = cache.Categories
            .Select(c => new BlockCategory(
                c.Name,
                c.DisplayName,
                c.IsSubCategory,
                c.Items.Select(i => new BlockId(i.TypeId, i.SubtypeId)).ToList()))
            .ToList();
    }

    async Task RebuildCacheAsync(
        string localizationPath,
        string categoryPath,
        string cubeBlocksDir,
        string cacheDir,
        string dataPath,
        string metaPath,
        IReadOnlyList<string> sources)
    {
        var overlay = new BusyOverlayViewModel("Updating Space Engineers data cache...");
        var shellTask = shell.ShowBusyOverlayAsync(overlay);

        try
        {
            await Task.Run(() =>
            {
                var localization = SpaceEngineersDataParser.LoadLocalization(localizationPath);
                var blocks = SpaceEngineersDataParser.LoadBlocks(cubeBlocksDir, localization);
                var categories = SpaceEngineersDataParser.LoadCategories(categoryPath, localization);

                Directory.CreateDirectory(cacheDir);

                var cacheData = new BlocksCacheData
                {
                    Blocks = blocks.Values.Select(b => new BlockInfoData
                    {
                        TypeId = b.Id.TypeId,
                        SubtypeId = b.Id.SubtypeId,
                        DisplayName = b.DisplayName,
                        IconPath = b.IconPath,
                        CubeSize = b.CubeSize,
                        Dlc = b.Dlc
                    }).ToList(),
                    Categories = categories.Select(c => new BlockCategoryData
                    {
                        Name = c.Name,
                        DisplayName = c.DisplayName,
                        IsSubCategory = c.IsSubCategory,
                        Items = c.Items
                            .Select(i => new BlockIdData { TypeId = i.TypeId, SubtypeId = i.SubtypeId })
                            .ToList()
                    }).ToList()
                };

                var meta = new BlocksCacheMeta
                {
                    Sources = sources.ToDictionary(
                        s => s,
                        s => File.Exists(s) ? File.GetLastWriteTimeUtc(s).ToString("O") : string.Empty)
                };

                File.WriteAllText(dataPath, JsonSerializer.Serialize(cacheData, JsonOptions));
                File.WriteAllText(metaPath, JsonSerializer.Serialize(meta, JsonOptions));

                _blocks = blocks;
                _categories = categories;
            });
        }
        finally
        {
            overlay.Dismiss();
            await shellTask;
        }
    }
}

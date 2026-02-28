using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.BlockDefinitions;

/// <summary>
///     Parses Space Engineers SBC files and RESX localization to provide block definition data.
/// </summary>
[Singleton<IBlockDefinitionService>]
public class BlockDefinitionService(ILogger logger) : IBlockDefinitionService
{
    // TODO: Must be resolved before release — detect SE installation path and allow manual config.
    // See .research/se-cubeblocks-format.md for data pipeline details.
    const string ContentPath = @"E:\Steam\steamapps\common\SpaceEngineers\Content";

    readonly SemaphoreSlim _lock = new(1, 1);
    bool _loaded;
    string? _loadError;
    ApiErrorKind _loadErrorKind;
    Dictionary<string, string> _localization = new();
    Dictionary<BlockId, BlockInfo> _blocks = new();
    List<BlockCategory> _categories = new();

    /// <inheritdoc />
    public async Task<ApiResult<IReadOnlyList<BlockCategory>>> GetCategoriesAsync()
    {
        if (!await EnsureLoadedAsync())
            return ApiResult<IReadOnlyList<BlockCategory>>.Fail(_loadError!, _loadErrorKind);
        return ApiResult<IReadOnlyList<BlockCategory>>.Ok(_categories);
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
        try
        {
            if (!Directory.Exists(ContentPath))
            {
                _loadError = $"Space Engineers Content folder not found at: {ContentPath}";
                _loadErrorKind = ApiErrorKind.Unavailable;
                logger.Warning(_loadError);
                return;
            }

            var localizationPath = Path.Combine(ContentPath, "Data", "Localization", "MyTexts.resx");
            var categoryPath = Path.Combine(ContentPath, "Data", "BlockCategories.sbc");
            var cubeBlocksDir = Path.Combine(ContentPath, "Data", "CubeBlocks");

            _localization = await LoadLocalizationAsync(localizationPath);
            _blocks = await LoadBlocksAsync(cubeBlocksDir);
            _categories = await LoadCategoriesAsync(categoryPath);
            _loaded = true;

            logger.Info($"Block definitions loaded: {_blocks.Count} blocks, {_categories.Count} categories.");
        }
        catch (Exception ex)
        {
            _loadError = $"Failed to load block definitions: {ex.Message}";
            _loadErrorKind = ApiErrorKind.ParseError;
            logger.Error(_loadError, ex);
        }
    }

    static Task<Dictionary<string, string>> LoadLocalizationAsync(string path) =>
        Task.Run(() =>
        {
            if (!File.Exists(path))
                return new Dictionary<string, string>();

            var doc = XDocument.Load(path);
            return doc.Root!
                .Elements("data")
                .Where(e => e.Attribute("name") != null)
                .ToDictionary(
                    e => e.Attribute("name")!.Value,
                    e => e.Element("value")?.Value ?? string.Empty);
        });

    Task<Dictionary<BlockId, BlockInfo>> LoadBlocksAsync(string directory) =>
        Task.Run(() =>
        {
            var result = new Dictionary<BlockId, BlockInfo>();
            if (!Directory.Exists(directory))
                return result;

            foreach (var file in Directory.GetFiles(directory, "*.sbc", SearchOption.AllDirectories))
            {
                try
                {
                    var doc = XDocument.Load(file);
                    foreach (var def in doc.Descendants("Definition"))
                    {
                        var idEl = def.Element("Id");
                        if (idEl == null)
                            continue;

                        var rawTypeId = idEl.Element("TypeId")?.Value;
                        if (string.IsNullOrWhiteSpace(rawTypeId))
                            continue;

                        var subtypeId = idEl.Element("SubtypeId")?.Value ?? string.Empty;
                        var id = BlockId.Parse($"{rawTypeId}/{subtypeId}");

                        var displayKey = def.Element("DisplayName")?.Value ?? string.Empty;
                        var displayName = _localization.GetValueOrDefault(displayKey, displayKey);
                        var iconPath = def.Element("Icon")?.Value;
                        var cubeSize = def.Element("CubeSize")?.Value ?? "Large";

                        result.TryAdd(id, new BlockInfo(id, displayName, iconPath, cubeSize));
                    }
                }
                catch (Exception ex)
                {
                    logger.Warning($"Skipping {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            return result;
        });

    Task<List<BlockCategory>> LoadCategoriesAsync(string path) =>
        Task.Run(() =>
        {
            var result = new List<BlockCategory>();
            if (!File.Exists(path))
                return result;

            var doc = XDocument.Load(path);
            foreach (var cat in doc.Descendants("Category"))
            {
                var name = cat.Element("Name")?.Value;
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // Skip categories not intended for placeable blocks (default is true)
                var isBlockCategoryStr = cat.Element("IsBlockCategory")?.Value;
                if (isBlockCategoryStr != null &&
                    string.Equals(isBlockCategoryStr, "false", StringComparison.OrdinalIgnoreCase))
                    continue;

                var rawDisplayKey = cat.Element("DisplayName")?.Value ?? name;
                var isSubCategory = rawDisplayKey.StartsWith("   ", StringComparison.Ordinal);
                var displayKey = rawDisplayKey.TrimStart();
                var displayName = _localization.GetValueOrDefault(displayKey, displayKey);

                var items = cat.Element("ItemIds")?
                    .Elements("string")
                    .Select(e => BlockId.Parse(e.Value))
                    .ToList() ?? [];

                result.Add(new BlockCategory(name, displayName, isSubCategory, items));
            }

            result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return result;
        });
}

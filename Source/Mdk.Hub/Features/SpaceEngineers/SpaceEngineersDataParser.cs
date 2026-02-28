using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Static parsing helpers for Space Engineers SBC and RESX data files.
///     Extracted for testability — all methods are pure transforms over file paths and dictionaries.
/// </summary>
internal static class SpaceEngineersDataParser
{
    /// <summary>
    ///     Loads the English localization dictionary from a <c>MyTexts.resx</c> file.
    ///     Returns an empty dictionary if the file does not exist.
    /// </summary>
    public static Dictionary<string, string> LoadLocalization(string path)
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
    }

    /// <summary>
    ///     Loads all block definitions from every <c>.sbc</c> file under <paramref name="directory" />.
    ///     Returns an empty dictionary if the directory does not exist.
    /// </summary>
    public static Dictionary<BlockId, BlockInfo> LoadBlocks(string directory, Dictionary<string, string> localization)
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
                    var displayName = localization.GetValueOrDefault(displayKey, displayKey);
                    var iconPath = def.Element("Icon")?.Value;
                    var cubeSize = def.Element("CubeSize")?.Value ?? "Large";
                    var dlc = def.Element("DLC")?.Value;

                    result.TryAdd(id, new BlockInfo(id, displayName, iconPath, cubeSize, dlc));
                }
            }
            catch (Exception ex)
            {
                // Swallow per-file parse errors so a single bad SBC does not block the rest.
                _ = ex;
            }
        }

        return result;
    }

    /// <summary>
    ///     Loads and sorts block categories from a <c>BlockCategories.sbc</c> file.
    ///     Returns an empty list if the file does not exist.
    ///     Categories with <c>IsBlockCategory=false</c> are excluded.
    /// </summary>
    public static List<BlockCategory> LoadCategories(string path, Dictionary<string, string> localization)
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

            // Skip categories not intended for placeable blocks (default is true).
            var isBlockCategoryStr = cat.Element("IsBlockCategory")?.Value;
            if (isBlockCategoryStr != null &&
                string.Equals(isBlockCategoryStr, "false", StringComparison.OrdinalIgnoreCase))
                continue;

            var rawDisplayKey = cat.Element("DisplayName")?.Value ?? name;
            var isSubCategory = rawDisplayKey.StartsWith("   ", StringComparison.Ordinal);
            var displayKey = rawDisplayKey.TrimStart();
            var displayName = localization.GetValueOrDefault(displayKey, displayKey);

            var items = cat.Element("ItemIds")?
                .Elements("string")
                .Select(e => BlockId.Parse(e.Value))
                .ToList() ?? [];

            result.Add(new BlockCategory(name, displayName, isSubCategory, items));
        }

        result.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        return result;
    }

    /// <summary>
    ///     Builds the list of source files used for cache staleness detection.
    /// </summary>
    public static List<string> BuildSourceFileList(string localizationPath, string categoryPath, string cubeBlocksDir)
    {
        var sources = new List<string> { localizationPath, categoryPath };
        if (Directory.Exists(cubeBlocksDir))
            sources.AddRange(Directory.GetFiles(cubeBlocksDir, "*.sbc", SearchOption.AllDirectories));
        return sources;
    }

    /// <summary>
    ///     Returns <c>true</c> when the cache meta file exists and all recorded source file timestamps match current disk state.
    /// </summary>
    public static bool IsCacheFresh(string metaPath, IReadOnlyList<string> sources)
    {
        if (!File.Exists(metaPath))
            return false;
        try
        {
            var meta = JsonSerializer.Deserialize<BlocksCacheMeta>(File.ReadAllText(metaPath));
            if (meta == null || meta.Sources.Count != sources.Count)
                return false;

            foreach (var source in sources)
            {
                if (!meta.Sources.TryGetValue(source, out var cached))
                    return false;
                var current = File.Exists(source)
                    ? File.GetLastWriteTimeUtc(source).ToString("O")
                    : string.Empty;
                if (current != cached)
                    return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

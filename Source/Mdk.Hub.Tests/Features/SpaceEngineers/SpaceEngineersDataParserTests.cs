using System.IO;
using System.Text.Json.Nodes;
using Mdk.Hub.Features.SpaceEngineers;

namespace Mdk.Hub.Tests.Features.SpaceEngineers;

/// <summary>
///     Tests for the static parsing helpers in <see cref="SpaceEngineersDataParser" />,
///     using entirely fictional fixture SBC/RESX files written to a temp directory.
/// </summary>
[TestFixture]
public class SpaceEngineersDataParserTests
{
    string _tempDir = null!;
    string _localizationPath = null!;
    string _categoryPath = null!;
    string _cubeBlocksDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mdk-hub-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var dataDir = Path.Combine(_tempDir, "Data");
        var locDir = Path.Combine(dataDir, "Localization");
        _cubeBlocksDir = Path.Combine(dataDir, "CubeBlocks");

        Directory.CreateDirectory(locDir);
        Directory.CreateDirectory(_cubeBlocksDir);

        _localizationPath = Path.Combine(locDir, "MyTexts.resx");
        _categoryPath = Path.Combine(dataDir, "BlockCategories.sbc");

        File.WriteAllText(_localizationPath, SeFixtures.LocalizationResx);
        File.WriteAllText(_categoryPath, SeFixtures.BlockCategoriesSbc);
        File.WriteAllText(Path.Combine(_cubeBlocksDir, "TestBlocks.sbc"), SeFixtures.CubeBlocksSbc);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Localization ──────────────────────────────────────────────────────────

    [Test]
    public void LoadLocalization_ReturnsAllKeys()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);

        TestContext.Out.WriteLine($"Loaded {loc.Count} localization entries:");
        foreach (var (key, value) in loc)
            TestContext.Out.WriteLine($"  {key} = \"{value}\"");

        Assert.That(loc, Does.ContainKey("DisplayName_TestRefinery"));
        Assert.That(loc["DisplayName_TestRefinery"], Is.EqualTo("Test Refinery"));
    }

    [Test]
    public void LoadLocalization_MissingFile_ReturnsEmpty()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(Path.Combine(_tempDir, "nonexistent.resx"));

        Assert.That(loc, Is.Empty);
    }

    // ── Block loading ─────────────────────────────────────────────────────────

    [Test]
    public void LoadBlocks_ResolvesDisplayNameViaLocalization()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var blocks = SpaceEngineersDataParser.LoadBlocks(_cubeBlocksDir, loc);

        TestContext.Out.WriteLine($"Loaded {blocks.Count} blocks:");
        foreach (var (blockId, info) in blocks)
            TestContext.Out.WriteLine($"  {blockId} → \"{info.DisplayName}\" ({info.CubeSize}) icon={info.IconPath ?? "<none>"}");

        var id = new BlockId("TestRefinery", "TestRefinery_Large");
        Assert.That(blocks, Does.ContainKey(id));
        Assert.That(blocks[id].DisplayName, Is.EqualTo("Test Refinery"));
    }

    [Test]
    public void LoadBlocks_StripsPrefixOnTypeId()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var blocks = SpaceEngineersDataParser.LoadBlocks(_cubeBlocksDir, loc);

        // Fixture has "MyObjectBuilder_TestArmor" as TypeId — should be normalised.
        var id = new BlockId("TestArmor", "TestArmor_Small");
        TestContext.Out.WriteLine($"Looking for normalised key: {id}");
        TestContext.Out.WriteLine($"All keys: {string.Join(", ", blocks.Keys)}");
        Assert.That(blocks, Does.ContainKey(id));
    }

    [Test]
    public void LoadBlocks_NullIconPath_WhenNoIconElement()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var blocks = SpaceEngineersDataParser.LoadBlocks(_cubeBlocksDir, loc);

        var id = new BlockId("TestArmor", "TestArmor_Small");
        Assert.That(blocks[id].IconPath, Is.Null);
    }

    [Test]
    public void LoadBlocks_PopulatesIconPath_WhenPresent()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var blocks = SpaceEngineersDataParser.LoadBlocks(_cubeBlocksDir, loc);

        var id = new BlockId("TestRefinery", "TestRefinery_Large");
        Assert.That(blocks[id].IconPath, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void LoadBlocks_MissingDirectory_ReturnsEmpty()
    {
        var blocks = SpaceEngineersDataParser.LoadBlocks(Path.Combine(_tempDir, "NoSuchDir"), new());

        Assert.That(blocks, Is.Empty);
    }

    // ── Category loading ──────────────────────────────────────────────────────

    [Test]
    public void LoadCategories_ExcludesIsBlockCategoryFalse()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var categories = SpaceEngineersDataParser.LoadCategories(_categoryPath, loc);

        Assert.That(categories.Select(c => c.Name), Does.Not.Contain("Section3_Excluded"));
    }

    [Test]
    public void LoadCategories_SortedAlphabeticallyByName()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var categories = SpaceEngineersDataParser.LoadCategories(_categoryPath, loc);

        TestContext.Out.WriteLine("Categories in returned order:");
        foreach (var c in categories)
            TestContext.Out.WriteLine($"  [{c.Name}] \"{c.DisplayName}\" sub={c.IsSubCategory} items={c.Items.Count}");

        var names = categories.Select(c => c.Name).ToList();
        Assert.That(names, Is.EqualTo(names.OrderBy(n => n, StringComparer.Ordinal).ToList()));
    }

    [Test]
    public void LoadCategories_DetectsSubCategoryFromLeadingSpaces()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var categories = SpaceEngineersDataParser.LoadCategories(_categoryPath, loc);

        var sub = categories.FirstOrDefault(c => c.Name == "Section2_Motion_Sub");
        TestContext.Out.WriteLine($"Sub-category: name=\"{sub?.Name}\" displayName=\"{sub?.DisplayName}\" isSubCategory={sub?.IsSubCategory}");
        Assert.That(sub, Is.Not.Null);
        Assert.That(sub!.IsSubCategory, Is.True);
    }

    [Test]
    public void LoadCategories_NormalCategoryIsNotSubCategory()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var categories = SpaceEngineersDataParser.LoadCategories(_categoryPath, loc);

        var normal = categories.FirstOrDefault(c => c.Name == "Section1_Processing");
        Assert.That(normal, Is.Not.Null);
        Assert.That(normal!.IsSubCategory, Is.False);
    }

    [Test]
    public void LoadCategories_DisplayNameStripsLeadingSpaces()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var categories = SpaceEngineersDataParser.LoadCategories(_categoryPath, loc);

        var sub = categories.First(c => c.Name == "Section2_Motion_Sub");
        TestContext.Out.WriteLine($"DisplayName repr: \"{sub.DisplayName}\" (length={sub.DisplayName.Length})");
        Assert.That(sub.DisplayName, Does.Not.StartWith(" "));
    }

    [Test]
    public void LoadCategories_ItemIdsAreParsed()
    {
        var loc = SpaceEngineersDataParser.LoadLocalization(_localizationPath);
        var categories = SpaceEngineersDataParser.LoadCategories(_categoryPath, loc);

        var processing = categories.First(c => c.Name == "Section1_Processing");
        Assert.That(processing.Items, Has.Count.EqualTo(1));
        Assert.That(processing.Items[0], Is.EqualTo(new BlockId("TestRefinery", "TestRefinery_Large")));
    }

    [Test]
    public void LoadCategories_MissingFile_ReturnsEmpty()
    {
        var categories = SpaceEngineersDataParser.LoadCategories(Path.Combine(_tempDir, "nope.sbc"), new());

        Assert.That(categories, Is.Empty);
    }

    // ── Cache staleness ───────────────────────────────────────────────────────

    [Test]
    public void IsCacheFresh_MissingMetaFile_ReturnsFalse()
    {
        var sources = SpaceEngineersDataParser.BuildSourceFileList(_localizationPath, _categoryPath, _cubeBlocksDir);

        var fresh = SpaceEngineersDataParser.IsCacheFresh(Path.Combine(_tempDir, "no-meta.json"), sources);

        Assert.That(fresh, Is.False);
    }

    [Test]
    public void IsCacheFresh_AfterWritingFreshMeta_ReturnsTrue()
    {
        var sources = SpaceEngineersDataParser.BuildSourceFileList(_localizationPath, _categoryPath, _cubeBlocksDir);
        var metaPath = Path.Combine(_tempDir, "meta.json");

        WriteFreshMeta(metaPath, sources);

        Assert.That(SpaceEngineersDataParser.IsCacheFresh(metaPath, sources), Is.True);
    }

    [Test]
    public void IsCacheFresh_AfterSourceFileModified_ReturnsFalse()
    {
        var sources = SpaceEngineersDataParser.BuildSourceFileList(_localizationPath, _categoryPath, _cubeBlocksDir);
        var metaPath = Path.Combine(_tempDir, "meta.json");

        WriteFreshMeta(metaPath, sources);

        // Touch a source file to advance its timestamp.
        File.SetLastWriteTimeUtc(_localizationPath, DateTime.UtcNow.AddSeconds(5));

        Assert.That(SpaceEngineersDataParser.IsCacheFresh(metaPath, sources), Is.False);
    }

    static void WriteFreshMeta(string metaPath, IReadOnlyList<string> sources)
    {
        var meta = new JsonObject();
        var sourcesObj = new JsonObject();
        foreach (var s in sources)
            sourcesObj[s] = File.Exists(s) ? File.GetLastWriteTimeUtc(s).ToString("O") : string.Empty;
        meta["Sources"] = sourcesObj;
        File.WriteAllText(metaPath, meta.ToJsonString());
    }
}

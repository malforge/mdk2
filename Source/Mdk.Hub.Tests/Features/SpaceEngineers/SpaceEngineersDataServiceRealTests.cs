using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.SpaceEngineers;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Tests.Features.SpaceEngineers;

/// <summary>
///     Integration tests that run against a real Space Engineers installation.
///     These tests are marked <see cref="ExplicitAttribute" /> and must be invoked manually —
///     they are skipped in normal CI runs.  They require Space Engineers to be installed and
///     detectable via the Steam registry on the current machine.
/// </summary>
[TestFixture]
[Explicit("Requires a real Space Engineers installation detected via the Steam registry.")]
public class SpaceEngineersDataServiceRealTests
{
    string _cacheDir = null!;
    FixedCacheDirStorageService _storage = null!;
    IShell _shell = null!;
    ILogger _logger = null!;
    SpaceEngineersDataService _service = null!;

    [SetUp]
    public void SetUp()
    {
        // Use a private temp cache dir so we don't pollute the real user cache.
        _cacheDir = Path.Combine(Path.GetTempPath(), $"mdk-hub-real-tests-{Guid.NewGuid():N}");
        _storage = new FixedCacheDirStorageService(_cacheDir);
        _shell = A.Fake<IShell>();
        _logger = A.Fake<ILogger>();

        _service = new SpaceEngineersDataService(_storage, _shell, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_cacheDir))
            Directory.Delete(_cacheDir, recursive: true);
    }

    [Test]
    [Description("Verifies that categories can be loaded from the real SE installation.")]
    public async Task GetCategoriesAsync_RealInstall_ReturnsCategories()
    {
        var result = await _service.GetCategoriesAsync();

        Assert.That(result.TryGetValue(out var categories), Is.True, result.ErrorMessage);
        TestContext.Out.WriteLine($"Loaded {categories!.Count} categories from real SE installation:");
        foreach (var c in categories)
            TestContext.Out.WriteLine($"  [{c.Name}] \"{c.DisplayName}\" sub={c.IsSubCategory} items={c.Items.Count}");
        Assert.That(categories, Is.Not.Empty);
    }

    [Test]
    [Description("Verifies that a well-known SE block type can be looked up by TypeId/SubtypeId.")]
    public async Task GetBlockAsync_RealInstall_LargeRefinery_ReturnsBlockInfo()
    {
        var result = await _service.GetBlockAsync("Refinery", "LargeRefinery");

        Assert.That(result.TryGetValue(out var block), Is.True, result.ErrorMessage);
        TestContext.Out.WriteLine($"Block: {block!.Id} → \"{block.DisplayName}\" ({block.CubeSize}) icon={block.IconPath ?? "<none>"}");
        Assert.That(block.DisplayName, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    [Description("Verifies that a second load re-uses the on-disk cache without re-parsing.")]
    public async Task GetCategoriesAsync_SecondService_UsesCachedData()
    {
        // First service — builds cache from source SBC files.
        var r1 = await _service.GetCategoriesAsync();
        Assert.That(r1.IsSuccess, Is.True, r1.ErrorMessage);

        var cacheDataPath = Path.Combine(_cacheDir, "se-blocks-data.json");
        var cacheBefore = File.GetLastWriteTimeUtc(cacheDataPath);
        TestContext.Out.WriteLine($"Cache written at: {cacheBefore:O}");

        // Small pause to ensure file timestamps differ if cache is re-written.
        await Task.Delay(50);

        // Second service — should load from cache without touching source files.
        var service2 = new SpaceEngineersDataService(_storage, _shell, _logger);
        var r2 = await service2.GetCategoriesAsync();
        Assert.That(r2.IsSuccess, Is.True, r2.ErrorMessage);

        var cacheAfter = File.GetLastWriteTimeUtc(cacheDataPath);
        TestContext.Out.WriteLine($"Cache timestamp after second load: {cacheAfter:O}");
        Assert.That(cacheAfter, Is.EqualTo(cacheBefore), "Cache file should not be re-written when still fresh.");

        Assert.That(r2.TryGetValue(out var c2), Is.True);
        TestContext.Out.WriteLine($"Second load returned {c2!.Count} categories.");
    }

    [Test]
    [Description("Dumps the first 20 block TypeIds loaded from the real installation.")]
    public async Task GetBlocksAsync_RealInstall_DumpsFirstBlocks()
    {
        // Load via categories so we can list blocks — the service doesn't expose a full block list,
        // but we can probe known blocks.  Instead we just verify a few common ones resolve.
        var catResult = await _service.GetCategoriesAsync();
        Assert.That(catResult.TryGetValue(out var categories), Is.True, catResult.ErrorMessage);

        // Walk the first category's items and resolve up to 20 blocks.
        var shown = 0;
        foreach (var category in categories!)
        {
            foreach (var item in category.Items)
            {
                var br = await _service.GetBlockAsync(item.TypeId, item.SubtypeId);
                if (br.TryGetValue(out var b))
                {
                    TestContext.Out.WriteLine($"  {b!.Id} → \"{b.DisplayName}\" ({b.CubeSize})");
                    shown++;
                }
                if (shown >= 20) break;
            }
            if (shown >= 20) break;
        }

        Assert.That(shown, Is.GreaterThan(0), "Expected to resolve at least one block from the first category.");
    }

    [Test]
    [Description("Verifies the terminal type scanner finds well-known terminal TypeIds in the real SE DLLs.")]
    public void TerminalScanner_RealInstall_FindsKnownTerminalTypes()
    {
        // Derive Bin64 path from the SE install — same logic as SpaceEngineersDataService.ResolveBinPath.
        var seInstall = new Mdk.Hub.Utility.SpaceEngineers();
        var contentPath = seInstall.GetInstallPath("Content");
        Assert.That(contentPath, Is.Not.Null, "SE Content path not found");

        var binPath = Path.Combine(Path.GetDirectoryName(contentPath)!, "Bin64");
        Assert.That(Directory.Exists(binPath), Is.True, $"Bin64 not found at {binPath}");

        var typeIds = SpaceEngineersTerminalScanner.Scan(binPath);

        TestContext.Out.WriteLine($"Found {typeIds.Count} terminal TypeIds. Sample:");
        foreach (var id in typeIds.OrderBy(x => x).Take(30))
            TestContext.Out.WriteLine($"  {id}");

        // These are known terminal blocks in every SE release.
        Assert.That(typeIds, Contains.Item("Refinery"),       "Refinery should be terminal");
        Assert.That(typeIds, Contains.Item("Gyro"),           "Gyro should be terminal");
        Assert.That(typeIds, Contains.Item("Thrust"),         "Thrust should be terminal");
        Assert.That(typeIds, Contains.Item("BatteryBlock"),   "BatteryBlock should be terminal");
    }

    [Test]
    [Description("Verifies that armor blocks (non-terminal) are NOT returned by the terminal scanner.")]
    public void TerminalScanner_RealInstall_ExcludesNonTerminalTypes()
    {
        var seInstall = new Mdk.Hub.Utility.SpaceEngineers();
        var contentPath = seInstall.GetInstallPath("Content");
        var binPath = Path.Combine(Path.GetDirectoryName(contentPath!)!, "Bin64");

        var typeIds = SpaceEngineersTerminalScanner.Scan(binPath);

        // CubeBlock (armor) is a structural block with no terminal interface.
        Assert.That(typeIds, Does.Not.Contain("CubeBlock"), "Armor CubeBlock should not be terminal");
        TestContext.Out.WriteLine($"Confirmed: CubeBlock not in terminal type set ({typeIds.Count} total terminal types).");
    }

    [Test]
    [Description("Verifies the full service load on real data only returns terminal blocks.")]
    public async Task GetCategoriesAsync_RealInstall_OnlyContainsTerminalBlocks()
    {
        var result = await _service.GetCategoriesAsync();
        Assert.That(result.TryGetValue(out var categories), Is.True, result.ErrorMessage);

        // Armor TypeId is "CubeBlock" — it should not appear in any category.
        foreach (var c in categories!)
        {
            var armorItems = c.Items.Where(i => i.TypeId == "CubeBlock").ToList();
            if (armorItems.Count > 0)
                TestContext.Out.WriteLine($"WARNING: Category {c.Name} still contains CubeBlock items: {string.Join(", ", armorItems)}");
            Assert.That(armorItems, Is.Empty, $"Category {c.Name} should not contain non-terminal CubeBlock items");
        }

        TestContext.Out.WriteLine($"All {categories.Count} categories contain only terminal blocks. ✓");
    }

    /// <summary>
    ///     Storage service that always returns <paramref name="cacheDir" /> for
    ///     <see cref="IFileStorageService.GetLocalApplicationDataPath" />, so the test uses its own
    ///     isolated cache folder.
    /// </summary>
    sealed class FixedCacheDirStorageService(string cacheDir) : FileStorageService
    {
        public override string GetLocalApplicationDataPath(params string[] subPaths) => cacheDir;
    }
}

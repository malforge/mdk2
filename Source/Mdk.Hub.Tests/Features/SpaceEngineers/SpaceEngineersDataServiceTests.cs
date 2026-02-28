using System.IO;
using System.Threading.Tasks;
using FakeItEasy;
using Mdk.Hub.Features.Diagnostics;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Features.SpaceEngineers;
using Mdk.Hub.Features.Storage;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Tests.Features.SpaceEngineers;

/// <summary>
///     Integration tests for <see cref="SpaceEngineersDataService" /> using a real temp directory
///     populated with fictional fixture files.  The SE installation path is injected via a test
///     subclass so no actual Space Engineers installation is required.
/// </summary>
[TestFixture]
public class SpaceEngineersDataServiceTests
{
    string _tempDir = null!;
    string _contentDir = null!;
    InMemoryFileStorageService _storage = null!;
    IShell _shell = null!;
    ILogger _logger = null!;
    TestableSpaceEngineersDataService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"mdk-hub-svc-tests-{Guid.NewGuid():N}");
        _contentDir = Path.Combine(_tempDir, "Content");

        // Build fixture content directory structure.
        var dataDir = Path.Combine(_contentDir, "Data");
        var locDir = Path.Combine(dataDir, "Localization");
        var cubeBlocksDir = Path.Combine(dataDir, "CubeBlocks");

        Directory.CreateDirectory(locDir);
        Directory.CreateDirectory(cubeBlocksDir);

        File.WriteAllText(Path.Combine(locDir, "MyTexts.resx"), SeFixtures.LocalizationResx);
        File.WriteAllText(Path.Combine(dataDir, "BlockCategories.sbc"), SeFixtures.BlockCategoriesSbc);
        File.WriteAllText(Path.Combine(cubeBlocksDir, "TestBlocks.sbc"), SeFixtures.CubeBlocksSbc);

        _storage = new InMemoryFileStorageService();
        _shell = A.Fake<IShell>();
        _logger = A.Fake<ILogger>();

        _service = new TestableSpaceEngineersDataService(_contentDir, _storage, _shell, _logger);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task GetCategoriesAsync_FirstCall_ReturnsCategories()
    {
        var result = await _service.GetCategoriesAsync();

        Assert.That(result.TryGetValue(out var categories), Is.True);
        TestContext.Out.WriteLine($"Got {categories!.Count} categories:");
        foreach (var c in categories)
            TestContext.Out.WriteLine($"  [{c.Name}] \"{c.DisplayName}\" sub={c.IsSubCategory} items={c.Items.Count}");
        Assert.That(categories, Is.Not.Empty);
    }

    [Test]
    public async Task GetCategoriesAsync_ExcludesIsBlockCategoryFalse()
    {
        var result = await _service.GetCategoriesAsync();

        Assert.That(result.TryGetValue(out var categories), Is.True);
        Assert.That(categories!.Select(c => c.Name), Does.Not.Contain("Section3_Excluded"));
    }

    [Test]
    public async Task GetBlockAsync_KnownBlock_ReturnsBlockInfo()
    {
        var result = await _service.GetBlockAsync("TestRefinery", "TestRefinery_Large");

        Assert.That(result.TryGetValue(out var block), Is.True);
        TestContext.Out.WriteLine($"Block: {block!.Id} → \"{block.DisplayName}\" ({block.CubeSize}) icon={block.IconPath ?? "<none>"}");
        Assert.That(block.DisplayName, Is.EqualTo("Test Refinery"));
    }

    [Test]
    public async Task GetBlockAsync_UnknownBlock_ReturnsFailure()
    {
        var result = await _service.GetBlockAsync("DoesNotExist", "Nope");

        TestContext.Out.WriteLine($"IsSuccess={result.IsSuccess} ErrorKind={result.ErrorKind} Message=\"{result.ErrorMessage}\"");
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorKind, Is.EqualTo(ApiErrorKind.NotFound));
    }

    [Test]
    public async Task GetCategoriesAsync_SecondCall_ReturnsSameData()
    {
        var first = await _service.GetCategoriesAsync();
        var second = await _service.GetCategoriesAsync();

        Assert.That(first.TryGetValue(out var c1), Is.True);
        Assert.That(second.TryGetValue(out var c2), Is.True);
        Assert.That(c1!.Count, Is.EqualTo(c2!.Count));
    }

    [Test]
    public async Task GetCategoriesAsync_WritesCacheFiles()
    {
        await _service.GetCategoriesAsync();

        var cacheDir = _storage.GetLocalApplicationDataPath("cache");
        // Cache was written to disk (real temp dir) not to the in-memory storage,
        // because the service uses File I/O directly for cache files.
        var dataPath = Path.Combine(cacheDir, "se-blocks-data.json");
        var metaPath = Path.Combine(cacheDir, "se-blocks-meta.json");

        Assert.That(File.Exists(dataPath), Is.True, "se-blocks-data.json should exist after first load");
        Assert.That(File.Exists(metaPath), Is.True, "se-blocks-meta.json should exist after first load");
    }

    [Test]
    public async Task GetCategoriesAsync_CacheMiss_ThenHit_BothSucceed()
    {
        // First call — cache miss, rebuild.
        var first = await _service.GetCategoriesAsync();
        Assert.That(first.IsSuccess, Is.True);

        // Second service instance with same cache dir — should hit cache.
        var service2 = new TestableSpaceEngineersDataService(_contentDir, _storage, _shell, _logger);
        var second = await service2.GetCategoriesAsync();

        Assert.That(second.IsSuccess, Is.True);
        Assert.That(second.TryGetValue(out var c2), Is.True);
        Assert.That(c2, Is.Not.Empty);
    }

    /// <summary>
    ///     Subclass that bypasses the SE install detection and injects a fixed content path.
    ///     The cache location comes from the real <c>IFileStorageService.GetLocalApplicationDataPath</c>,
    ///     which on <see cref="InMemoryFileStorageService" /> returns a predictable in-memory path.
    ///     However, the service writes cache files via <c>File.WriteAllText</c>, so the cache ends
    ///     up in the path string that <see cref="InMemoryFileStorageService" /> returns — which is
    ///     a real filesystem-looking path that we can observe with <c>File.Exists</c>.
    /// </summary>
    sealed class TestableSpaceEngineersDataService(
        string contentPath,
        InMemoryFileStorageService storage,
        IShell shell,
        ILogger logger)
        : SpaceEngineersDataService(storage, shell, logger)
    {
        protected override string? ResolveContentPath() => contentPath;
    }
}

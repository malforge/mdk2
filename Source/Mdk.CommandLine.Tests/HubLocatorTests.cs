using System;
using System.IO;
using FakeItEasy;
using Mdk.CommandLine;
using Mdk.CommandLine.Shared.Api;
using NUnit.Framework;

namespace MDK.CommandLine.Tests;

[TestFixture]
public class HubLocatorTests
{
    string _workDir = null!;
    string _pathFile = null!;
    string _fakeHubExe = null!;
    IConsole _console = null!;

    [SetUp]
    public void Setup()
    {
        _workDir = Path.Combine(Path.GetTempPath(), $"mdk-hublocator-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_workDir);
        _pathFile = Path.Combine(_workDir, "hub.path");
        _fakeHubExe = Path.Combine(_workDir, "FakeHub.exe");
        _console = A.Fake<IConsole>();
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_workDir))
            Directory.Delete(_workDir, recursive: true);
    }

    [Test]
    public void LocateHub_WhenPathFileMissing_ReportsMissing()
    {
        var result = HubLocator.LocateHub(_console, _pathFile);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.Path, Is.Null);
            Assert.That(result.PathFile, Is.EqualTo(_pathFile));
            Assert.That(result.PathFileExists, Is.False);
            Assert.That(result.PathFileContents, Is.Null);
            Assert.That(result.TargetExists, Is.False);
        });
    }

    [Test]
    public void LocateHub_WhenPathFileEmpty_ReportsEmpty()
    {
        File.WriteAllText(_pathFile, "");

        var result = HubLocator.LocateHub(_console, _pathFile);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.PathFileExists, Is.True);
            Assert.That(result.PathFileContents, Is.Empty);
            Assert.That(result.TargetExists, Is.False);
        });
    }

    [Test]
    public void LocateHub_WhenTargetMissing_ReportsStalePath()
    {
        var stalePath = Path.Combine(_workDir, "does-not-exist.exe");
        File.WriteAllText(_pathFile, stalePath);

        var result = HubLocator.LocateHub(_console, _pathFile);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.False);
            Assert.That(result.PathFileExists, Is.True);
            Assert.That(result.PathFileContents, Is.EqualTo(stalePath));
            Assert.That(result.TargetExists, Is.False);
        });
    }

    [Test]
    public void LocateHub_WhenAllPresent_ReturnsPath()
    {
        File.WriteAllText(_fakeHubExe, "");
        File.WriteAllText(_pathFile, _fakeHubExe);

        var result = HubLocator.LocateHub(_console, _pathFile);

        Assert.Multiple(() =>
        {
            Assert.That(result.Found, Is.True);
            Assert.That(result.Path, Is.EqualTo(_fakeHubExe));
            Assert.That(result.PathFileExists, Is.True);
            Assert.That(result.PathFileContents, Is.EqualTo(_fakeHubExe));
            Assert.That(result.TargetExists, Is.True);
        });
    }

    [Test]
    public void LocateHub_TrimsWhitespaceFromPathFile()
    {
        File.WriteAllText(_fakeHubExe, "");
        File.WriteAllText(_pathFile, "\r\n  " + _fakeHubExe + "  \r\n");

        var result = HubLocator.LocateHub(_console, _pathFile);

        Assert.That(result.Path, Is.EqualTo(_fakeHubExe));
    }

    [Test]
    public void GenerateHelpHtml_WithoutLocation_OmitsDiagnostics()
    {
        var html = HubLocator.GenerateHelpHtml(location: null);

        Assert.That(html, Does.Not.Contain("Diagnostics"));
    }

    [Test]
    public void GenerateHelpHtml_WithMissingPathFile_ShowsMarkerAndMissingStatus()
    {
        var location = new HubLocation(null, _pathFile, false, null, false, null);

        var html = HubLocator.GenerateHelpHtml(location);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain("Diagnostics"));
            Assert.That(html, Does.Contain(_pathFile));
            Assert.That(html, Does.Contain("missing"));
            // Contents row and target row should not appear when the marker file doesn't exist
            Assert.That(html, Does.Not.Contain("Contents"));
            Assert.That(html, Does.Not.Contain("Hub executable"));
        });
    }

    [Test]
    public void GenerateHelpHtml_WithStalePath_ShowsStaleTargetMissing()
    {
        var stalePath = Path.Combine(_workDir, "old-hub.exe");
        var location = new HubLocation(null, _pathFile, true, stalePath, false, null);

        var html = HubLocator.GenerateHelpHtml(location);

        Assert.Multiple(() =>
        {
            Assert.That(html, Does.Contain(_pathFile));
            Assert.That(html, Does.Contain(stalePath));
            Assert.That(html, Does.Contain("Hub executable"));
            Assert.That(html, Does.Contain("missing"));
        });
    }

    [Test]
    public void GenerateHelpHtml_HtmlEncodesPathFileContents()
    {
        var sneakyPath = @"C:\Path\<script>alert(1)</script>";
        var location = new HubLocation(null, _pathFile, true, sneakyPath, false, null);

        var html = HubLocator.GenerateHelpHtml(location);

        Assert.That(html, Does.Not.Contain("<script>alert(1)</script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.Commands.Pack;
using Mdk.CommandLine.Utility;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
///     Information about how the project should be packed.
/// </summary>
public class ScriptProjectMetadata
{
    static readonly char[] IgnoresSeparator = [';'];
    readonly ImmutableList<string>? _ignores;
    readonly int? _indentSize;
    readonly bool? _interactive;
    readonly ImmutableDictionary<string, string>? _macros;
    readonly Version? _mdkProjectVersion;
    readonly MinifierLevel? _minify;
    readonly string? _outputDirectory;
    readonly ImmutableHashSet<string>? _preprocessorMacros;
    readonly string? _projectFileName;
    readonly bool? _trimTypes;
    string? _projectDirectory;
    readonly Matcher? _matcher;

    ScriptProjectMetadata(Version? mdkProjectVersion, string? projectFileName, string? outputDirectory, MinifierLevel? minify, ImmutableList<string>? ignores, bool? trimTypes, int? indentSize, ImmutableDictionary<string, string>? macros, ImmutableHashSet<string>? preprocessorMacros, bool? interactive, bool isClosed = false)
    {
        _projectFileName = projectFileName;
        _projectDirectory = Path.GetDirectoryName(projectFileName);
        _mdkProjectVersion = mdkProjectVersion;
        _outputDirectory = outputDirectory;
        _minify = minify;
        _ignores = ignores;
        _trimTypes = trimTypes;
        _indentSize = indentSize;
        _macros = macros;
        _preprocessorMacros = preprocessorMacros;
        _interactive = interactive;
        if (isClosed)
        {
            var matcher = new Matcher();
            if (_ignores != null)
                foreach (var ignore in _ignores)
                    matcher.AddInclude(ignore);
            
            _matcher = matcher;
        }
    }

    /// <summary>
    ///     Which version of MDK the project is expecting.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Version MdkProjectVersion => _mdkProjectVersion ?? throw new InvalidOperationException("MDK project version not set");

    /// <summary>
    ///     The name of the project file.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public string ProjectFileName => _projectFileName ?? throw new InvalidOperationException("Project file name not set");

    /// <summary>
    ///     The directory to output the packed script to.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public string OutputDirectory => _outputDirectory ?? throw new InvalidOperationException("Output directory not set");

    /// <summary>
    ///     The level of minification to apply to the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public MinifierLevel Minify => _minify ?? throw new InvalidOperationException("Minify not set");

    // /// <summary>
    // ///     The list of files and folders to ignore when packing the script.
    // /// </summary>
    // /// <exception cref="InvalidOperationException"></exception>
    // public ImmutableList<FileSystemInfo> Ignores => _ignores ?? throw new InvalidOperationException("Ignores not set");
    public ImmutableList<string> Ignores => _ignores ?? throw new InvalidOperationException("Ignores not set");

    /// <summary>
    ///     Whether to trim unused types from the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public bool TrimTypes => _trimTypes ?? throw new InvalidOperationException("Trim types not set");

    /// <summary>
    ///     The number of spaces to use for indentation.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public int IndentSize => _indentSize ?? throw new InvalidOperationException("Indent size not set");

    /// <summary>
    ///     The macros to apply to the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public ImmutableDictionary<string, string> Macros => _macros ?? throw new InvalidOperationException("Macros not set");

    /// <summary>
    ///     The preprocessor macros to apply to the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public ImmutableHashSet<string> PreprocessorMacros => _preprocessorMacros ?? throw new InvalidOperationException("Preprocessor macros not set");

    /// <summary>
    ///     Whether to run the tool in interactive mode.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Interactive => _interactive ?? throw new InvalidOperationException("Interactive not set");

    /// <summary>
    ///     The directory containing the project file.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public string ProjectDirectory
    {
        get
        {
            _projectDirectory ??= Path.GetDirectoryName(ProjectFileName);
            return _projectDirectory ?? throw new InvalidOperationException("Project directory not set");
        }
    }

    /// <summary>
    ///     Add additional ignores to the metadata.
    /// </summary>
    /// <param name="ignore"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithAdditionalIgnore(string ignore) =>
        new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores == null ? ImmutableList.Create(ignore) : _ignores.Add(ignore), _trimTypes, _indentSize, _macros, _preprocessorMacros, _interactive);

    // /// <summary>
    // ///     Add additional ignores to the metadata.
    // /// </summary>
    // /// <param name="ignore"></param>
    // /// <returns></returns>
    // public ScriptProjectMetadata WithAdditionalIgnore(FileSystemInfo ignore) =>
    //     new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores == null ? ImmutableList.Create(ignore) : _ignores.Add(ignore), _trimTypes, _indentSize, _macros, _preprocessorMacros, _interactive);
    //
    /// <summary>
    ///     Change the output directory for the metadata.
    /// </summary>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithOutputDirectory(string outputDirectory) =>
        new(_mdkProjectVersion, _projectFileName, outputDirectory, _minify, _ignores, _trimTypes, _indentSize, _macros, _preprocessorMacros, _interactive);

    /// <summary>
    ///     Add additional macros to the metadata.
    /// </summary>
    /// <param name="macros"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithAdditionalMacros(IDictionary<string, string> macros) =>
        new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores, _trimTypes, _indentSize, _macros == null ? macros.ToImmutableDictionary() : _macros.AddRange(macros), _preprocessorMacros, _interactive);

    /// <summary>
    ///     Add additional preprocessor macros to the metadata.
    /// </summary>
    /// <param name="preprocessorMacros"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithAdditionalPreprocessorMacros(IEnumerable<string> preprocessorMacros) =>
        new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores, _trimTypes, _indentSize, _macros, _preprocessorMacros == null ? preprocessorMacros.ToImmutableHashSet() : _preprocessorMacros.Union(preprocessorMacros), _interactive);

    /// <summary>
    ///     Apply the other metadata to this metadata, overwriting any set values with the other's values, or combining them
    ///     where possible.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public ScriptProjectMetadata ApplyOther(ScriptProjectMetadata other)
    {
        var outputDirectory = _outputDirectory;

        if (outputDirectory != null)
        {
            if (other._outputDirectory != null && !string.Equals(other._outputDirectory, "auto", StringComparison.OrdinalIgnoreCase))
                outputDirectory = other._outputDirectory;
        }
        else
            outputDirectory = other._outputDirectory;

        return new ScriptProjectMetadata(other._mdkProjectVersion,
            other._projectFileName,
            outputDirectory,
            other._minify ?? _minify,
            _ignores == null ? other._ignores : other._ignores == null ? _ignores : _ignores.AddRange(other._ignores),
            other._trimTypes ?? _trimTypes,
            other._indentSize ?? _indentSize,
            _macros == null ? other._macros : other._macros == null ? _macros : _macros.AddRange(other._macros),
            _preprocessorMacros == null ? other._preprocessorMacros : other._preprocessorMacros == null ? _preprocessorMacros : _preprocessorMacros.Union(other._preprocessorMacros),
            other._interactive ?? _interactive
        );
    }

    /// <summary>
    ///     Finalizes the metadata and closes it for further modification. Defaults will be applied to any unset values - if
    ///     possible.
    /// </summary>
    /// <returns></returns>
    public ScriptProjectMetadata Close(Func<string>? resolveAutoOutputDirectory = null)
    {
        var outputDirectory = _outputDirectory ?? "auto";
        if (string.Equals(outputDirectory, "auto", StringComparison.OrdinalIgnoreCase))
            outputDirectory = resolveAutoOutputDirectory?.Invoke() ?? Path.Combine(ProjectDirectory, "IngameScripts", "local");

        return new ScriptProjectMetadata(
            _mdkProjectVersion,
            Path.GetFullPath(_projectFileName ?? throw new InvalidOperationException("Project file name not set")),
            Path.GetFullPath(outputDirectory ?? throw new InvalidOperationException("Output directory not set")),
            _minify ?? MinifierLevel.None,
            _ignores ?? ImmutableList<string>.Empty,
            _trimTypes ?? false,
            _indentSize ?? 4,
            _macros ?? ImmutableDictionary<string, string>.Empty,
            _preprocessorMacros ?? ImmutableHashSet<string>.Empty,
            _interactive ?? false,
            true
        );
    }


    /// <summary>
    ///     Loads metadata from the given legacy MDK1 project files.
    /// </summary>
    /// <param name="project"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<ScriptProjectMetadata?> LoadLegacyAsync(Project project)
    {
        if (project.FilePath == null) return null;
        var rootPath = Path.GetDirectoryName(project.FilePath)!;
        if (!project.TryFindDocument("mdk.options.props", out var mdkOptionsProps)) return null;
        string? outputDirectory = null;
        var source = await mdkOptionsProps.GetTextAsync();
        var document = XDocument.Parse(source.ToString());
        var ns = document.Root?.Name.Namespace;
        if (ns == null) return null;

        var mdkVersionString = document.Root?.FindByPath(XNames.PropertyGroup, XNames.MDKVersion)?.Value;
        if (mdkVersionString == null || !Version.TryParse(mdkVersionString, out var mdkVersion))
            return null;
        var mdkTrimTypesString = document.Root?.FindByPath(XNames.PropertyGroup, XNames.MDKTrimTypes, XNames.Enabled)?.Value;
        var mdkTrimTypes = string.Equals(mdkTrimTypesString, "yes", StringComparison.OrdinalIgnoreCase);
        var mdkMinifyString = document.Root?.FindByPath(XNames.PropertyGroup, XNames.MDKMinify, XNames.Level)?.Value;
        if (mdkMinifyString == null || !Enum.TryParse<MinifierLevel>(mdkMinifyString, true, out var mdkMinify))
            return null;
        var ignoreElement = document.Root?.FindByPath(XNames.PropertyGroup, XNames.MDKIgnore);
        var mdkIgnores = (ignoreElement?.Elements(ns + "Folder").Select(e => getDirectoryGlob(e.Value)) ?? Enumerable.Empty<string>())
            .Concat(ignoreElement?.Elements(ns + "Folder").Elements(ns + "File").Select(e => getFileGlob(e.Value)) ?? Enumerable.Empty<string>()).ToImmutableList();

        if (project.TryFindDocument("mdk.paths.props", out var mdkPathsProps))
        {
            source = await mdkPathsProps.GetTextAsync();
            document = XDocument.Parse(source.ToString());
            ns = document.Root?.Name.Namespace;
            if (ns != null)
            {
                var outputPath = document.Root?.FindByPath(XNames.PropertyGroup, XNames.MDKOuputPath)?.Value;
                if (!string.IsNullOrWhiteSpace(outputPath))
                    outputDirectory = Path.Combine(rootPath, outputPath);
            }
        }

        return new ScriptProjectMetadata(
            mdkVersion,
            project.FilePath,
            outputDirectory,
            mdkMinify,
            mdkIgnores,
            mdkTrimTypes,
            null,
            null,
            null,
            true
        );

        // FileSystemInfo getDirectoryInfo(string path)
        // {
        //     if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Invalid MDK ignore folder");
        //     return new DirectoryInfo(Path.Combine(rootPath, path));
        // }

        string getDirectoryGlob(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Invalid MDK ignore folder");
            return Path.Combine(rootPath, path, "**", "*");
        }

        // FileSystemInfo getFileInfo(string path)
        // {
        //     if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Invalid MDK ignore file");
        //     return new FileInfo(Path.Combine(rootPath, path));
        // }

        string getFileGlob(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Invalid MDK ignore file");
            return Path.Combine(rootPath, path);
        }
    }

    /// <summary>
    ///     Create a new metadata object from the provided <see cref="PackParameters" />.
    /// </summary>
    /// <param name="options"></param>
    /// <param name="mdkVersion"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static ScriptProjectMetadata ForOptions(PackParameters options, Version mdkVersion) =>
        new(
            mdkVersion,
            options.ProjectFile ?? throw new InvalidOperationException("No project file specified"),
            options.Output,
            options.MinifierLevel,
            null,
            options.TrimUnusedTypes,
            null,
            null,
            null,
            options.Global?.Interactive ?? false
        );

    /// <summary>
    ///     Load a new metadata object from ini files derived from the project file name. Returns an empty metadata object if
    ///     no ini files are found.
    /// </summary>
    /// <param name="projectFileName"></param>
    /// <returns></returns>
    public static ScriptProjectMetadata? FromIni(string projectFileName)
    {
        var iniFileName = Path.ChangeExtension(projectFileName, ".mdk.ini");
        var localIniFileName = Path.ChangeExtension(projectFileName, ".mdk.local.ini");

        if (!File.Exists(iniFileName) && !File.Exists(localIniFileName))
            return null;

        var output = (string?)null;
        var minifier = (MinifierLevel?)null;
        var trim = (bool?)null;
        var interactive = (bool?)null;
        var ignores = (ImmutableList<string>?)null;

        if (File.Exists(iniFileName))
        {
            var ini = Ini.FromFile(iniFileName);

            var parameters = ini["parameters"];
            output = parameters["output"].ToString();
            minifier = parameters["minifier"].ToEnum<MinifierLevel>();
            trim = parameters["trim"].ToBool();
            interactive = parameters["interactive"].ToBool();
            var ignoresString = parameters["ignore"].ToString();
            if (!string.IsNullOrWhiteSpace(ignoresString))
                ignores = ignoresString.Split(IgnoresSeparator, StringSplitOptions.RemoveEmptyEntries).ToImmutableList();
        }

        if (File.Exists(localIniFileName))
        {
            var ini = Ini.FromFile(localIniFileName);

            var parameters = ini["parameters"];
            if (parameters.TryGet("output", out string v1))
                output = v1;
            if (parameters.TryGet("minifier", out MinifierLevel v2))
                minifier = v2;
            if (parameters.TryGet("trim", out bool v3))
                trim = v3;
            if (parameters.TryGet("interactive", out bool v4))
                interactive = v4;
            if (parameters.TryGet("ignore", out string v5) && !string.IsNullOrWhiteSpace(v5))
                ignores = v5.Split(IgnoresSeparator, StringSplitOptions.RemoveEmptyEntries).ToImmutableList();
        }

        return new ScriptProjectMetadata(null, projectFileName, output, minifier, ignores, trim, null, null, null, interactive);
    }

    /// <summary>
    ///     Generate a string representation of the metadata (for debugging, human-readable output).
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var builder = new StringBuilder();
        if (_mdkProjectVersion != null)
            builder.AppendLine($"MDK Project Version: {_mdkProjectVersion}");
        if (_projectFileName != null)
            builder.AppendLine($"Project File Name: {_projectFileName}");
        if (_outputDirectory != null)
            builder.AppendLine($"Output Directory: {_outputDirectory}");
        if (_minify != null && _minify != MinifierLevel.None)
            builder.AppendLine($"Minify: {_minify}");
        if (_ignores is { Count: > 0 })
            builder.AppendLine($"Ignores: {string.Join(", ", _ignores)}");
        if (_trimTypes != null && _trimTypes.Value)
            builder.AppendLine($"Trim Types: {_trimTypes}");
        if (_indentSize != null)
            builder.AppendLine($"Indent Size: {_indentSize}");
        if (_macros is { Count: > 0 })
            builder.AppendLine($"Macros: {string.Join(", ", _macros.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");
        if (_preprocessorMacros is { Count: > 0 })
            builder.AppendLine($"Preprocessor Macros: {string.Join(", ", _preprocessorMacros)}");
        if (_interactive != null && _interactive.Value)
            builder.AppendLine($"Interactive: {_interactive}");
        return builder.ToString();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    static class XNames
    {
        public static readonly XName PropertyGroup = XName.Get("PropertyGroup", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName MDKVersion = XName.Get("MDKVersion", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName MDKTrimTypes = XName.Get("MDKTrimTypes", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName MDKMinify = XName.Get("MDKMinify", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName MDKIgnore = XName.Get("MDKIgnore", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName MDKOuputPath = XName.Get("MDKOutputPath", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName Enabled = XName.Get("Enabled", "http://schemas.microsoft.com/developer/msbuild/2003");
        public static readonly XName Level = XName.Get("Level", "http://schemas.microsoft.com/developer/msbuild/2003");
    }

    /// <summary>
    /// Whether the given path should be ignored.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool ShouldIgnore(string path)
    {
        var matcher = _matcher ?? throw new InvalidOperationException("Metadata not closed");
        var rootDir = Path.GetDirectoryName(ProjectFileName) ?? throw new InvalidOperationException("Project directory not set");
        return matcher.Match(rootDir, path).HasMatches;
    }
}
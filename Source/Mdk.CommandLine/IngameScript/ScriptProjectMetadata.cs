using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript;

/// <summary>
/// Information about how the project should be packed.
/// </summary>
public class ScriptProjectMetadata
{
    readonly ImmutableList<FileSystemInfo>? _ignores;
    readonly int? _indentSize;
    readonly ImmutableDictionary<string, string>? _macros;
    readonly Version _mdkProjectVersion;
    readonly MinifierLevel? _minify;
    readonly string? _outputDirectory;
    readonly ImmutableHashSet<string>? _preprocessorMacros;
    readonly string _projectFileName;
    readonly bool? _trimTypes;
    string? _projectDirectory;
    readonly bool? _toClipboard;

    ScriptProjectMetadata(Version mdkProjectVersion, string projectFileName, string? outputDirectory, MinifierLevel? minify, ImmutableList<FileSystemInfo>? ignores, bool? trimTypes, int? indentSize, ImmutableDictionary<string, string>? macros, ImmutableHashSet<string>? preprocessorMacros, bool? toClipboard)
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
        _toClipboard = toClipboard;
    }

    /// <summary>
    /// Which version of MDK the project is expecting.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public Version MdkProjectVersion => _mdkProjectVersion ?? throw new InvalidOperationException("MDK project version not set");
    
    /// <summary>
    ///    The name of the project file.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public string ProjectFileName => _projectFileName ?? throw new InvalidOperationException("Project file name not set");
    
    /// <summary>
    ///    The directory to output the packed script to.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public string OutputDirectory => _outputDirectory ?? throw new InvalidOperationException("Output directory not set");
    
    /// <summary>
    ///   The level of minification to apply to the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public MinifierLevel Minify => _minify ?? throw new InvalidOperationException("Minify not set");
    
    /// <summary>
    ///   The list of files and folders to ignore when packing the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public ImmutableList<FileSystemInfo> Ignores => _ignores ?? throw new InvalidOperationException("Ignores not set");
    
    /// <summary>
    ///  Whether to trim unused types from the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public bool TrimTypes => _trimTypes ?? throw new InvalidOperationException("Trim types not set");
    
    /// <summary>
    ///    The number of spaces to use for indentation.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public int IndentSize => _indentSize ?? throw new InvalidOperationException("Indent size not set");
    
    /// <summary>
    ///   The macros to apply to the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public ImmutableDictionary<string, string> Macros => _macros ?? throw new InvalidOperationException("Macros not set");
    
    /// <summary>
    ///  The preprocessor macros to apply to the script.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public ImmutableHashSet<string> PreprocessorMacros => _preprocessorMacros ?? throw new InvalidOperationException("Preprocessor macros not set");
    
    /// <summary>
    ///  Whether to copy the packed script to the clipboard.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public bool ToClipboard => _toClipboard ?? throw new InvalidOperationException("To clipboard not set");

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
    public ScriptProjectMetadata WithAdditionalIgnore(FileSystemInfo ignore) =>
        new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores == null ? ImmutableList.Create(ignore) : _ignores.Add(ignore), _trimTypes, _indentSize, _macros, _preprocessorMacros, _toClipboard);

    /// <summary>
    ///     Change the output directory for the metadata.
    /// </summary>
    /// <param name="outputDirectory"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithOutputDirectory(string outputDirectory) =>
        new(_mdkProjectVersion, _projectFileName, outputDirectory, _minify, _ignores, _trimTypes, _indentSize, _macros, _preprocessorMacros, _toClipboard);

    /// <summary>
    ///     Add additional macros to the metadata.
    /// </summary>
    /// <param name="macros"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithAdditionalMacros(IDictionary<string, string> macros) =>
        new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores, _trimTypes, _indentSize, _macros == null ? macros.ToImmutableDictionary() : _macros.AddRange(macros), _preprocessorMacros, _toClipboard);

    /// <summary>
    /// Add additional preprocessor macros to the metadata.
    /// </summary>
    /// <param name="preprocessorMacros"></param>
    /// <returns></returns>
    public ScriptProjectMetadata WithAdditionalPreprocessorMacros(IEnumerable<string> preprocessorMacros) =>
        new(_mdkProjectVersion, _projectFileName, _outputDirectory, _minify, _ignores, _trimTypes, _indentSize, _macros, _preprocessorMacros == null ? preprocessorMacros.ToImmutableHashSet() : _preprocessorMacros.Union(preprocessorMacros), _toClipboard);
    
    /// <summary>
    ///     Apply the other metadata to this metadata, overwriting any set values with the other's values, or combining them
    ///     where possible.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public ScriptProjectMetadata ApplyOther(ScriptProjectMetadata other) =>
        new(other._mdkProjectVersion,
            other._projectFileName,
            other._outputDirectory ?? _outputDirectory,
            other._minify ?? _minify,
            _ignores == null ? other._ignores : other._ignores == null ? _ignores : _ignores.AddRange(other._ignores),
            other._trimTypes ?? _trimTypes,
            other._indentSize ?? _indentSize,
            _macros == null ? other._macros : other._macros == null ? _macros : _macros.AddRange(other._macros),
            _preprocessorMacros == null ? other._preprocessorMacros : other._preprocessorMacros == null ? _preprocessorMacros : _preprocessorMacros.Union(other._preprocessorMacros),
            other._toClipboard ?? _toClipboard
        );

    /// <summary>
    ///     Finalizes the metadata and closes it for further modification. Defaults will be applied to any unset values - if
    ///     possible.
    /// </summary>
    /// <returns></returns>
    public ScriptProjectMetadata Close() =>
        new(_mdkProjectVersion,
            _projectFileName,
            _outputDirectory ?? Path.Combine(ProjectDirectory, "IngameScripts", "local"),
            _minify ?? MinifierLevel.None,
            _ignores ?? ImmutableList<FileSystemInfo>.Empty,
            _trimTypes ?? false,
            _indentSize ?? 4,
            _macros ?? ImmutableDictionary<string, string>.Empty,
            _preprocessorMacros ?? ImmutableHashSet<string>.Empty,
            _toClipboard ?? false
        );


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
        var propertyGroup = document.Root?.Element(ns + "PropertyGroup");
        var mdkVersionString = propertyGroup?.Element(ns + "MDKVersion")?.Value;
        if (mdkVersionString == null || !Version.TryParse(mdkVersionString, out var mdkVersion))
            return null;
        var mdkTrimTypes = string.Equals(propertyGroup?.Element(ns + "MDKTrimTypes")?.Element(ns + "Enabled")?.Value, "yes", StringComparison.OrdinalIgnoreCase);
        var mdkMinifyString = propertyGroup?.Element(ns + "MDKMinify")?.Element(ns + "Level")?.Value;
        if (mdkMinifyString == null || !Enum.TryParse<MinifierLevel>(mdkMinifyString, true, out var mdkMinify))
            return null;
        var mdkIgnores = (propertyGroup?.Element(ns + "MDKIgnore")?.Elements(ns + "Folder").Select(e => getDirectoryInfo(e.Value)) ?? Enumerable.Empty<FileSystemInfo>())
            .Concat(propertyGroup?.Element(ns + "MDKIgnore")?.Elements(ns + "File").Select(e => getFileInfo(e.Value)) ?? Enumerable.Empty<FileSystemInfo>()).ToImmutableList();

        if (project.TryFindDocument("mdk.paths.props", out var mdkPathsProps))
        {
            source = await mdkPathsProps.GetTextAsync();
            document = XDocument.Parse(source.ToString());
            ns = document.Root?.Name.Namespace;
            if (ns != null)
            {
                // <MDKOutputPath>E:\Data\sesaves\IngameScripts</MDKOutputPath>
                var outputPath = document.Root?.Element(ns + "MDKOutputPath")?.Value;
                if (!string.IsNullOrWhiteSpace(outputPath))
                    outputDirectory = Path.Combine(rootPath, outputPath);
            }
        }

        return new ScriptProjectMetadata(
            mdkProjectVersion: mdkVersion,
            projectFileName: project.FilePath,
            outputDirectory: outputDirectory,
            minify: mdkMinify,
            ignores: mdkIgnores,
            trimTypes: mdkTrimTypes,
            indentSize: null,
            macros: null,
            preprocessorMacros: null,
            toClipboard: null
        );

        FileSystemInfo getDirectoryInfo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Invalid MDK ignore folder");
            return new DirectoryInfo(Path.Combine(rootPath, path));
        }

        FileSystemInfo getFileInfo(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("Invalid MDK ignore file");
            return new FileInfo(Path.Combine(rootPath, path));
        }
    }
    
    public static ScriptProjectMetadata ForOptions(PackOptions options, Version mdkVersion) =>
        new(
            mdkProjectVersion: mdkVersion,
            projectFileName: options.ProjectFile ?? throw new InvalidOperationException("No project file specified"),
            outputDirectory: options.Output ?? Path.Combine(Path.GetDirectoryName(options.ProjectFile)!, "IngameScripts", "local"),
            minify: options.MinifierLevel,
            ignores: null,
            trimTypes: options.TrimUnusedTypes,
            indentSize: null,
            macros: null,
            preprocessorMacros: null,
            toClipboard: options.ToClipboard
        );
}
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript;

public class ScriptProjectMetadata
{
    public required Version MdkProjectVersion { get; init; }
    public MinifierLevel Minify { get; init; }
    public ImmutableArray<FileSystemInfo> Ignores { get; init; }
    public bool TrimTypes { get; init; }
    public int IndentSize { get; init; } = 4;
    
    public ScriptProjectMetadata WithAdditionalIgnore(FileSystemInfo ignore)
    {
        return new ScriptProjectMetadata
        {
            MdkProjectVersion = MdkProjectVersion,
            Minify = Minify,
            Ignores = Ignores.IsDefault? ImmutableArray.Create(ignore) : Ignores.Add(ignore),
            TrimTypes = TrimTypes
        };
    }

    public static async Task<ScriptProjectMetadata?> LoadAsync(Project project)
    {
        if (project.FilePath == null) return null;
        var rootPath = Path.GetDirectoryName(project.FilePath)!;

        if (!project.TryFindDocument("mdk.options.props", out var mdkOptionsProps)) return null;
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
            .Concat(propertyGroup?.Element(ns + "MDKIgnore")?.Elements(ns + "File").Select(e => getFileInfo(e.Value)) ?? Enumerable.Empty<FileSystemInfo>()).ToImmutableArray();

        return new ScriptProjectMetadata
        {
            MdkProjectVersion = mdkVersion,
            TrimTypes = mdkTrimTypes,
            Minify = mdkMinify,
            Ignores = mdkIgnores
        };

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
}
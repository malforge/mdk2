using System;
using System.Collections.Immutable;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Configuration;

public class ProjectData
{
    /// <summary>
    /// The name of the project. This is not necessarily the same as the name of the .csproj file, and is not guaranteed to be unique. It is intended for display purposes only.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    ///     The raw INI data from mdk.ini (null if file doesn't exist).
    /// </summary>
    public Ini? MainIni { get; init; }

    /// <summary>
    ///     The raw INI data from mdk.local.ini (null if file doesn't exist).
    /// </summary>
    public Ini? LocalIni { get; init; }

    /// <summary>
    ///     Path to the mdk.ini file (if it exists).
    /// </summary>
    public string? MainIniPath { get; init; }

    /// <summary>
    ///     Path to the mdk.local.ini file (if it exists).
    /// </summary>
    public string? LocalIniPath { get; init; }

    /// <summary>
    ///     Path to the project (.csproj) file.
    /// </summary>
    public required CanonicalPath ProjectPath { get; init; }

    /// <summary>
    ///     The layered project configuration, as loaded from <see cref="MainIni" /> and <see cref="LocalIni" />.
    /// </summary>
    public required ProjectConfig Config { get; init; }
}

public class ProjectConfig
{
    /// <summary>
    ///     A layer representing the default settings that apply to all projects (not loaded from any file).
    /// </summary>
    public ProjectConfigLayer Default { get; init; } = new()
    {
        Type = null, // Type must be set explicitly in INI
        Interactive = InteractiveMode.OpenHub,
        Trace = false,
        Minify = MinifierLevel.None,
        MinifyExtraOptions = MinifierExtraOptions.None,
        Ignores = ImmutableArray.Create("obj/**/*", "MDK/**/*", "**/*.debug.cs"),
        Namespaces = ImmutableArray.Create("IngameScript"),
        Output = null,
        BinaryPath = null
    };

    /// <summary>
    ///     If set: the main project configuration layer, typically loaded from mdk.ini.
    /// </summary>
    public ProjectConfigLayer? Main { get; init; } = new();

    /// <summary>
    ///     If set: the local project configuration layer, typically loaded from mdk.local.ini.
    /// </summary>
    public ProjectConfigLayer? Local { get; init; } = new();

    /// <summary>
    ///     Returns a single layer with the effective values based on layer priority (Local > Main > Default).
    /// </summary>
    public ProjectConfigLayer GetEffective() =>
        new()
        {
            Type = Local?.Type ?? Main?.Type ?? Default.Type,
            Interactive = Local?.Interactive ?? Main?.Interactive ?? Default.Interactive,
            Trace = Local?.Trace ?? Main?.Trace ?? Default.Trace,
            Minify = Local?.Minify ?? Main?.Minify ?? Default.Minify,
            MinifyExtraOptions = Local?.MinifyExtraOptions ?? Main?.MinifyExtraOptions ?? Default.MinifyExtraOptions,
            Ignores = Local?.Ignores ?? Main?.Ignores ?? Default.Ignores,
            Namespaces = Local?.Namespaces ?? Main?.Namespaces ?? Default.Namespaces,
            Output = Local?.Output ?? Main?.Output ?? Default.Output,
            BinaryPath = Local?.BinaryPath ?? Main?.BinaryPath ?? Default.BinaryPath
        };

    /// <summary>
    ///     Optimizes the configuration by removing redundant settings. If something is set in Local that is the same as in
    ///     Main, it is removed from Local.
    ///     If something is not set in Main, it will be filled in from Default.
    /// </summary>
    /// <returns></returns>
    public ProjectConfig Optimize()
    {
        var optimizedMain = new ProjectConfigLayer
        {
            Type = Main?.Type ?? Default.Type,
            Interactive = Main?.Interactive ?? Default.Interactive,
            Trace = Main?.Trace ?? Default.Trace,
            Minify = Main?.Minify ?? Default.Minify,
            MinifyExtraOptions = Main?.MinifyExtraOptions ?? Default.MinifyExtraOptions,
            Ignores = Main?.Ignores ?? Default.Ignores,
            Namespaces = Main?.Namespaces ?? Default.Namespaces,
            Output = Main?.Output,
            BinaryPath = Main?.BinaryPath
        };

        var optimizedLocal = new ProjectConfigLayer
        {
            Type = Local?.Type != optimizedMain.Type ? Local?.Type : null,
            Interactive = Local?.Interactive != optimizedMain.Interactive ? Local?.Interactive : null,
            Trace = Local?.Trace != optimizedMain.Trace ? Local?.Trace : null,
            Minify = Local?.Minify != optimizedMain.Minify ? Local?.Minify : null,
            MinifyExtraOptions = Local?.MinifyExtraOptions != optimizedMain.MinifyExtraOptions ? Local?.MinifyExtraOptions : null,
            Ignores = !CompareIgnores(Local?.Ignores, optimizedMain.Ignores) ? Local?.Ignores : null,
            Namespaces = !CompareNamespaces(Local?.Namespaces, optimizedMain.Namespaces) ? Local?.Namespaces : null,
            Output = !ComparePaths(Local?.Output, optimizedMain.Output) ? Local?.Output : null,
            BinaryPath = !ComparePaths(Local?.BinaryPath, optimizedMain.BinaryPath) ? Local?.BinaryPath : null
        };

        return new ProjectConfig
        {
            Default = Default,
            Main = optimizedMain,
            Local = optimizedLocal
        };
    }

    public static bool CompareNamespaces(ImmutableArray<string>? a, ImmutableArray<string>? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        if (a.Value.Length != b.Value.Length)
            return false;
        for (var i = 0; i < a.Value.Length; i++)
        {
            if (!string.Equals(a.Value[i], b.Value[i], StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    public static bool CompareIgnores(ImmutableArray<string>? a, ImmutableArray<string>? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;
        if (a.Value.Length != b.Value.Length)
            return false;
        for (var i = 0; i < a.Value.Length; i++)
        {
            if (!string.Equals(a.Value[i], b.Value[i], StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    public static bool ComparePaths(CanonicalPath? a, CanonicalPath? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;

        return a.Value.Equals(b.Value);
    }
}

public class ProjectConfigLayer : IEquatable<ProjectConfigLayer>
{
    /// <summary>
    ///     What type of project this is.
    /// </summary>
    public ProjectType? Type { get; init; }

    /// <summary>
    ///     Interactive mode setting: how the user wants to interact with builds.
    /// </summary>
    public InteractiveMode? Interactive { get; init; }

    /// <summary>
    ///     Whether to enable trace logging.
    /// </summary>
    public bool? Trace { get; init; }

    /// <summary>
    ///     What type, if any, minification to perform (only applies to script projects).
    /// </summary>
    public MinifierLevel? Minify { get; init; }

    /// <summary>
    ///     When minifying, extra options to apply (only applies to script projects).
    /// </summary>
    public MinifierExtraOptions? MinifyExtraOptions { get; init; }

    /// <summary>
    ///     A list of glob patterns for files to ignore.
    /// </summary>
    public ImmutableArray<string>? Ignores { get; init; }

    /// <summary>
    ///     A list of namespaces to accept without warnings (only applies to script projects).
    /// </summary>
    public ImmutableArray<string>? Namespaces { get; init; }

    /// <summary>
    ///     The output path for the built script or mod. Null means "auto" (determine automatically).
    /// </summary>
    public CanonicalPath? Output { get; init; }

    /// <summary>
    ///     Where to find the game binaries. Null means "auto" (determine automatically).
    /// </summary>
    public CanonicalPath? BinaryPath { get; init; }

    /// <summary>
    ///     Compares two layers for semantic equality (uses type-aware comparison for collections and paths).
    /// </summary>
    public bool Equals(ProjectConfigLayer? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Type == other.Type
               && Interactive == other.Interactive
               && Trace == other.Trace
               && Minify == other.Minify
               && MinifyExtraOptions == other.MinifyExtraOptions
               && ProjectConfig.CompareIgnores(Ignores, other.Ignores)
               && ProjectConfig.CompareNamespaces(Namespaces, other.Namespaces)
               && ProjectConfig.ComparePaths(Output, other.Output)
               && ProjectConfig.ComparePaths(BinaryPath, other.BinaryPath);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((ProjectConfigLayer)obj);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Type);
        hashCode.Add(Interactive);
        hashCode.Add(Trace);
        hashCode.Add(Minify);
        hashCode.Add(MinifyExtraOptions);
        hashCode.Add(Ignores);
        hashCode.Add(Namespaces);
        hashCode.Add(Output);
        hashCode.Add(BinaryPath);
        return hashCode.ToHashCode();
    }

    public static bool operator ==(ProjectConfigLayer? left, ProjectConfigLayer? right) => Equals(left, right);

    public static bool operator !=(ProjectConfigLayer? left, ProjectConfigLayer? right) => !Equals(left, right);
}

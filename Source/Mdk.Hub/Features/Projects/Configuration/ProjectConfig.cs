using System;
using System.Collections.Immutable;
using Mdk.Hub.Features.Projects.Overview;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
/// Represents a complete project configuration with multiple layers (Default, Main, Local) that can be combined to determine effective settings.
/// </summary>
public class ProjectConfig
{
    /// <summary>
    ///     A layer representing the default settings that apply to all projects (not loaded from any file).
    /// </summary>
    public ProjectConfigLayer Default { get; init; } = new()
    {
        Type = null, // Type must be set explicitly in INI
        Interactive = InteractiveMode.ShowNotification,
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

    /// <summary>
    /// Compares two namespace arrays for equality using case-sensitive ordinal comparison.
    /// </summary>
    /// <param name="a">The first namespace array to compare.</param>
    /// <param name="b">The second namespace array to compare.</param>
    /// <returns>True if both arrays are null or contain the same namespaces in the same order; otherwise, false.</returns>
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

    /// <summary>
    /// Compares two ignore pattern arrays for equality using case-insensitive ordinal comparison.
    /// </summary>
    /// <param name="a">The first ignore pattern array to compare.</param>
    /// <param name="b">The second ignore pattern array to compare.</param>
    /// <returns>True if both arrays are null or contain the same patterns in the same order (case-insensitive); otherwise, false.</returns>
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

    /// <summary>
    /// Compares two canonical paths for equality.
    /// </summary>
    /// <param name="a">The first path to compare.</param>
    /// <param name="b">The second path to compare.</param>
    /// <returns>True if both paths are null or represent the same path; otherwise, false.</returns>
    public static bool ComparePaths(CanonicalPath? a, CanonicalPath? b)
    {
        if (a == null && b == null)
            return true;
        if (a == null || b == null)
            return false;

        return a.Value.Equals(b.Value);
    }
}
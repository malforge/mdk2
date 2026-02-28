using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace Mdk.Hub.Features.SpaceEngineers;

/// <summary>
///     Uses Mono.Cecil to statically analyse Space Engineers DLLs and identify which block TypeIds
///     correspond to terminal blocks (i.e., accessible via <c>IMyTerminalBlock</c> in scripts).
///     <para>
///         A block type is considered a terminal block when its implementation class carries both
///         <c>[MyCubeBlockTypeAttribute]</c> (which maps it to a TypeId) and
///         <c>[MyTerminalInterfaceAttribute]</c> (which marks it as terminal-accessible).
///     </para>
/// </summary>
internal static class SpaceEngineersTerminalScanner
{
    const string TerminalInterfaceAttributeName = "MyTerminalInterfaceAttribute";
    const string CubeBlockTypeAttributeName = "MyCubeBlockTypeAttribute";
    const string ObjectBuilderPrefix = "MyObjectBuilder_";

    /// <summary>
    ///     Returns the paths to the two SE DLLs that are scanned for terminal block types.
    /// </summary>
    public static string[] GetDllPaths(string binPath) =>
    [
        Path.Combine(binPath, "Sandbox.Game.dll"),
        Path.Combine(binPath, "SpaceEngineers.Game.dll")
    ];

    /// <summary>
    ///     Scans SE DLLs in <paramref name="binPath" /> and returns the set of TypeIds that
    ///     correspond to terminal block types.  Returns an empty set if the DLLs are not found
    ///     or cannot be read.
    /// </summary>
    public static HashSet<string> Scan(string binPath)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dll in GetDllPaths(binPath))
        {
            if (!File.Exists(dll))
                continue;
            try
            {
                using var module = ModuleDefinition.ReadModule(dll);
                foreach (var type in module.GetTypes())
                    TryAddTerminalTypeId(type, result);
            }
            catch
            {
                // Skip any DLL that can't be read (e.g., wrong format, locked file).
            }
        }
        return result;
    }

    static void TryAddTerminalTypeId(TypeDefinition type, HashSet<string> result)
    {
        var hasTerminalInterface = false;
        string? typeId = null;

        foreach (var attr in type.CustomAttributes)
        {
            switch (attr.AttributeType.Name)
            {
                case TerminalInterfaceAttributeName:
                    hasTerminalInterface = true;
                    break;

                case CubeBlockTypeAttributeName:
                    if (attr.ConstructorArguments.Count > 0 &&
                        attr.ConstructorArguments[0].Value is TypeReference typeRef)
                    {
                        var name = typeRef.Name;
                        typeId = name.StartsWith(ObjectBuilderPrefix, StringComparison.Ordinal)
                            ? name[ObjectBuilderPrefix.Length..]
                            : name;
                    }
                    break;
            }
        }

        if (hasTerminalInterface && typeId != null)
            result.Add(typeId);
    }
}

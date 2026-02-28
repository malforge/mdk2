using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mdk.Hub.Tests.Features.SpaceEngineers;

/// <summary>
///     Builds minimal fake SE game DLLs on disk so the terminal scanner can be tested without a
///     real Space Engineers installation.
///     <para>
///         The generated DLL contains fictional <c>MyCubeBlockTypeAttribute</c> and
///         <c>MyTerminalInterfaceAttribute</c> types (matching only by name, not namespace) and
///         block types decorated with them, matching exactly what the real scanner looks for.
///     </para>
/// </summary>
internal static class FakeGameDllBuilder
{
    /// <summary>
    ///     Creates a fake <c>FakeGame.dll</c> in <paramref name="directory" /> that contains
    ///     one or more block types.  Each entry specifies a TypeId and whether the block is terminal.
    /// </summary>
    /// <param name="directory">Output directory (must already exist).</param>
    /// <param name="blockTypes">Pairs of (typeId, isTerminal).</param>
    /// <returns>Full path to the written DLL.</returns>
    public static string Create(string directory, params (string TypeId, bool IsTerminal)[] blockTypes)
    {
        var assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition("FakeGame", new Version(1, 0, 0, 0)),
            "FakeGame", ModuleKind.Dll);
        var module = assembly.MainModule;

        var typeTypeRef = module.ImportReference(typeof(Type));

        // Define MyCubeBlockTypeAttribute — ctor takes a System.Type
        var cubeAttrType = new TypeDefinition("", "MyCubeBlockTypeAttribute",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        var cubeAttrCtor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        cubeAttrCtor.Parameters.Add(new ParameterDefinition(typeTypeRef));
        cubeAttrCtor.Body.GetILProcessor().Emit(OpCodes.Ret);
        cubeAttrType.Methods.Add(cubeAttrCtor);
        module.Types.Add(cubeAttrType);

        // Define MyTerminalInterfaceAttribute — parameterless ctor
        var termAttrType = new TypeDefinition("", "MyTerminalInterfaceAttribute",
            TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
        var termAttrCtor = new MethodDefinition(".ctor",
            MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
            module.TypeSystem.Void);
        termAttrCtor.Body.GetILProcessor().Emit(OpCodes.Ret);
        termAttrType.Methods.Add(termAttrCtor);
        module.Types.Add(termAttrType);

        foreach (var (typeId, isTerminal) in blockTypes)
        {
            // ObjectBuilder type — name is what the scanner strips the prefix from
            var obType = new TypeDefinition("", $"MyObjectBuilder_{typeId}",
                TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
            module.Types.Add(obType);

            // Block implementation type
            var blockType = new TypeDefinition("", $"My{typeId}",
                TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);

            var cubeAttr = new CustomAttribute(cubeAttrCtor);
            cubeAttr.ConstructorArguments.Add(new CustomAttributeArgument(typeTypeRef, obType));
            blockType.CustomAttributes.Add(cubeAttr);

            if (isTerminal)
                blockType.CustomAttributes.Add(new CustomAttribute(termAttrCtor));

            module.Types.Add(blockType);
        }

        var path = Path.Combine(directory, "Sandbox.Game.dll");
        assembly.Write(path);
        return path;
    }
}

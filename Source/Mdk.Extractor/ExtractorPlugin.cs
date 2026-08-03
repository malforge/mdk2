// Mdk.Extractor
//
// Copyright 2023-2026 The MDK² Authors

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Digi.BuildInfo.Features.LiveData;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;
using VRageMath;

namespace Mdk.Extractor;

/// <summary>
///     Runs inside a headless Space Engineers dedicated server and writes out everything MDK needs from the game.
/// </summary>
/// <remarks>
///     Started by <see cref="Extractor" />, which names this assembly in the server's plugin list and passes the
///     output paths through environment variables, since the server runs as a separate process.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ExtractorPlugin : IPlugin
{
    const string ObjectBuilderPrefix = "MyObjectBuilder_";

    readonly Dictionary<MyObjectBuilderType, List<string>> _subtypesByTypeId = new();

    string _modWhitelist;
    string _pbWhitelist;
    string _pbPrologue;
    string _terminal;
    bool _started;

    public void Dispose() { }

    public void Init(object gameInstance)
    {
        _modWhitelist = ReadPath(Extractor.ModWhitelistVariable);
        _pbWhitelist = ReadPath(Extractor.PbWhitelistVariable);
        _pbPrologue = ReadPath(Extractor.PbPrologueVariable);
        _terminal = ReadPath(Extractor.TerminalVariable);

        MyLog.Default.WriteLineAndConsole("MDK2 Extractor: Plugin loaded");
    }

    static string ReadPath(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    ///     Waits for the session to come up, then extracts once.
    /// </summary>
    /// <remarks>
    ///     A dedicated server never raises <see cref="MySession.AfterLoading" />, which is what the extractor
    ///     originally hung this off when it drove the game's own user interface. The session's own readiness flag
    ///     is the signal that does work in both cases.
    /// </remarks>
    public void Update()
    {
        if (_started)
            return;
        if (MySession.Static == null || !MySession.Static.Ready)
            return;

        _started = true;
        ExtractAsync();
    }

    async void ExtractAsync()
    {
        try
        {
            await GameThread.SwitchToGameThread();
            MySandboxGame.Config.ExperimentalMode = true;

            WriteWhitelists(_modWhitelist, _pbWhitelist);
            WritePrologue(_pbPrologue);
            await GrabTerminalAsync();
        }
        catch (Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: ERROR: extraction failed: {e}");
        }
        finally
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            await GameThread.SwitchToGameThread();
            MySandboxGame.ExitThreadSafe();
        }
    }

    void WriteWhitelists(string modWhitelist, string pbWhitelist)
    {
        var modTypes = string.IsNullOrEmpty(modWhitelist) ? null : new List<string>();
        var pbTypes = string.IsNullOrEmpty(pbWhitelist) ? null : new List<string>();

        if (modTypes == null && pbTypes == null)
            return;

        MyLog.Default.WriteLineAndConsole("MDK2 Extractor: Retrieving whitelist(s)");

        foreach (var item in MyScriptCompiler.Static.Whitelist.GetWhitelist())
        {
            if (modTypes != null && (item.Value & MyWhitelistTarget.ModApi) == MyWhitelistTarget.ModApi)
                modTypes.Add(item.Key);

            if (pbTypes != null && (item.Value & MyWhitelistTarget.Ingame) == MyWhitelistTarget.Ingame)
                pbTypes.Add(item.Key);
        }

        if (modTypes != null)
        {
            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: Writing mod whitelist {modTypes.Count} {modWhitelist}");
            File.WriteAllText(modWhitelist, string.Join(Environment.NewLine, modTypes));
        }

        if (pbTypes != null)
        {
            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: Writing pb whitelist {pbTypes.Count} {pbWhitelist}");
            File.WriteAllText(pbWhitelist, string.Join(Environment.NewLine, pbTypes));
        }
    }

    /// <summary>
    ///     Dumps the using directives the programmable block puts in front of every script.
    /// </summary>
    /// <remarks>
    ///     Taken from the wrapper the game generates around a script rather than from the fields that feed it. Those
    ///     fields turned out to be two separate mechanisms, plain namespace imports and type aliases, and there is no
    ///     guarantee a future version will not add a third. The generated source is where they all end up, so reading it
    ///     keeps this correct without anyone having to notice the change.
    ///     If the game stops producing something recognisable, this complains loudly and leaves the existing file alone
    ///     rather than writing a short list that would make the analyzer flag correct code.
    /// </remarks>
    void WritePrologue(string pbPrologue)
    {
        if (string.IsNullOrEmpty(pbPrologue))
            return;

        MyLog.Default.WriteLineAndConsole("MDK2 Extractor: Retrieving the ingame script prologue");

        var method = typeof(MyScriptCompiler).GetMethod("GetInGameScript", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        if (method == null)
        {
            MyLog.Default.WriteLineAndConsole("MDK2 Extractor: ERROR: MyScriptCompiler no longer has GetInGameScript. The prologue was NOT written.");
            return;
        }

        object script;
        try
        {
            script = method.Invoke(MyScriptCompiler.Static, new object[] { "", "Program", "MyGridProgram", "public" });
        }
        catch (Exception e)
        {
            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: ERROR: GetInGameScript threw {e.GetType().Name}. The prologue was NOT written.");
            return;
        }

        const BindingFlags anyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var code = script?.GetType().GetProperty("Code", anyInstance)?.GetValue(script) as string
                   ?? script?.GetType().GetField("Code", anyInstance)?.GetValue(script) as string;
        if (string.IsNullOrWhiteSpace(code))
        {
            MyLog.Default.WriteLineAndConsole("MDK2 Extractor: ERROR: GetInGameScript produced no code. The prologue was NOT written.");
            return;
        }

        // The prologue is the run of using directives the generated file opens with; everything after that is the
        // wrapper class and the script's own code.
        var prologue = new List<string>();
        foreach (var rawLine in code.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
                continue;
            if (!line.StartsWith("using ", StringComparison.Ordinal) || !line.EndsWith(";", StringComparison.Ordinal))
                break;
            prologue.Add(line);
        }

        if (prologue.Count == 0)
        {
            MyLog.Default.WriteLineAndConsole("MDK2 Extractor: ERROR: the generated script opens with no using directives. The prologue was NOT written.");
            return;
        }

        MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: Writing pb prologue {prologue.Count} directives {pbPrologue}");
        File.WriteAllText(pbPrologue, string.Join(Environment.NewLine, prologue));
    }

    async Task GrabTerminalAsync()
    {
        if (string.IsNullOrEmpty(_terminal))
            return;

        var result = await SpawnBlocksForAnalysisAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
        await GameThread.SwitchToGameThread();

        GrabTerminalActions(_terminal, result);
    }

    async Task<List<(MyCubeBlockDefinition, IMyTerminalBlock)>> SpawnBlocksForAnalysisAsync()
    {
        try
        {
            var byTypeId = new Dictionary<MyObjectBuilderType, MyCubeBlockDefinition>();
            _subtypesByTypeId.Clear();
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (definition is not MyCubeBlockDefinition cbd)
                    continue;

                // Blocks the game hides from the build menu. Debug spheres are the obvious case, but this also
                // keeps the sampled subtype of a mixed type honest: most wheel definitions are hidden, and
                // documenting a block from one of those would describe something nobody can place.
                if (!cbd.Public)
                    continue;

                // Every subtype sharing a type id, because only one of them is spawned and the actions and
                // properties read off it apply to all of them. Without this a consumer cannot tell which blocks
                // an entry actually covers.
                if (!_subtypesByTypeId.TryGetValue(cbd.Id.TypeId, out var subtypes))
                {
                    subtypes = new List<string>();
                    _subtypesByTypeId[cbd.Id.TypeId] = subtypes;
                }
                if (!string.IsNullOrEmpty(cbd.Id.SubtypeName))
                    subtypes.Add(cbd.Id.SubtypeName);

                if (byTypeId.TryGetValue(cbd.Id.TypeId, out var existing) && existing.CubeSize == MyCubeSize.Large)
                    continue;
                byTypeId[cbd.Id.TypeId] = cbd;
            }

            var largeDefs = new List<MyCubeBlockDefinition>();
            var smallDefs = new List<MyCubeBlockDefinition>();
            foreach (var def in byTypeId.Values)
            {
                if (def.CubeSize == MyCubeSize.Large)
                    largeDefs.Add(def);
                else
                    smallDefs.Add(def);
            }

            var blocks = new List<(MyCubeBlockDefinition, IMyTerminalBlock)>();
            if (largeDefs.Count > 0)
                blocks.AddRange(await SpawnGridAsync(largeDefs, MyCubeSize.Large, new Vector3D(100000, 0, 0)));
            if (smallDefs.Count > 0)
                blocks.AddRange(await SpawnGridAsync(smallDefs, MyCubeSize.Small, new Vector3D(100000, 0, 1000)));

            return blocks;
        }
        catch (ReflectionTypeLoadException e)
        {
            foreach (var loaderException in e.LoaderExceptions) MyLog.Default.Error(loaderException.ToString());
            throw;
        }
    }

    async Task<List<(MyCubeBlockDefinition, IMyTerminalBlock)>> SpawnGridAsync(
        List<MyCubeBlockDefinition> definitions,
        MyCubeSize gridSize,
        Vector3D spawnPos)
    {
        const int spacing = 16;
        var positioned = new List<(MyCubeBlockDefinition Definition, Vector3I Position)>(definitions.Count);
        for (var i = 0; i < definitions.Count; i++)
            positioned.Add((definitions[i], new Vector3I(i * spacing, 0, 0)));

        await GameThread.SwitchToGameThread();
        var tcs = new TaskCompletionSource<IMyCubeGrid>();
        TempBlockSpawn.Spawn(positioned, gridSize, spawnPos, grid => tcs.SetResult(grid));
        var spawnedGrid = await tcs.Task;

        var results = new List<(MyCubeBlockDefinition, IMyTerminalBlock)>();
        foreach (var (def, pos) in positioned)
        {
            var slim = spawnedGrid.GetCubeBlock(pos);
            if (slim?.FatBlock is IMyTerminalBlock terminal)
                results.Add((def, terminal));
        }
        return results;
    }

    void GrabTerminalActions(string terminalFileName, List<(MyCubeBlockDefinition, IMyTerminalBlock)> blocks)
    {
        try
        {
            MyLog.Default.WriteLineAndConsole("MDK2 Extractor: Extracting terminal actions and properties");

            var blockInfos = new List<BlockInfo>();
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (cbd, block) in blocks)
            {
                var typeDefinition = StripObjectBuilderPrefix(cbd.Id.TypeId.ToString());

                // One definition is sampled per type id, so a repeat would mean the game changed under us.
                if (!seen.Add(typeDefinition))
                {
                    MyLog.Default.Info($"Skipping a second definition for {typeDefinition}");
                    continue;
                }

                var actions = new List<ITerminalAction>();
                var properties = new List<ITerminalProperty>();
                block.GetActions(actions);
                block.GetProperties(properties);

                MyLog.Default.Info($"Got {actions.Count} actions and {properties.Count} properties from {cbd.Id}");

                _subtypesByTypeId.TryGetValue(cbd.Id.TypeId, out var subtypes);
                blockInfos.Add(new BlockInfo(
                    typeDefinition,
                    cbd.Id.SubtypeName,
                    subtypes ?? new List<string>(),
                    block.GetType(),
                    FindDeclaredInterface(block.GetType()),
                    FindIngameInterfaces(block.GetType()),
                    actions,
                    properties));
            }

            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: Writing terminal cache {blockInfos.Count} blocks {terminalFileName}");
            WriteTerminals(blockInfos, terminalFileName);
        }
        catch (ReflectionTypeLoadException e)
        {
            foreach (var loaderException in e.LoaderExceptions) MyLog.Default.Error(loaderException.ToString());
            throw;
        }
    }

    static string StripObjectBuilderPrefix(string name) =>
        name.StartsWith(ObjectBuilderPrefix, StringComparison.Ordinal) ? name.Substring(ObjectBuilderPrefix.Length) : name;

    /// <summary>
    ///     The ingame interface the game itself declares for a block, where it declares one.
    /// </summary>
    /// <remarks>
    ///     Kept for consumers that want the game's own opinion, but it is not load bearing: thirteen block classes
    ///     have no <c>MyTerminalInterfaceAttribute</c> at all, among them the rotor and the missile turret, and the
    ///     attribute is declared with <c>Inherited = false</c> so a base class never supplies one. Blocks are keyed
    ///     by type definition instead, and every interface they implement is listed separately.
    /// </remarks>
    static Type FindDeclaredInterface(Type blockType)
    {
        var attribute = blockType.GetCustomAttribute<MyTerminalInterfaceAttribute>();
        return attribute?.LinkedTypes.FirstOrDefault(t => t.Namespace?.EndsWith(".Ingame") ?? false);
    }

    /// <summary>
    ///     Every ingame interface a block implements, so a consumer can list a block under all of them.
    /// </summary>
    /// <remarks>
    ///     No attempt is made to pick a "primary" interface. A script can fetch a block through any interface it
    ///     implements, marker interfaces such as <c>IMyTextSurfaceProvider</c> included, so all of them are equally
    ///     real and choosing between them would be guesswork.
    /// </remarks>
    static List<Type> FindIngameInterfaces(Type blockType) =>
        blockType.GetInterfaces()
            .Where(i => i.Namespace?.EndsWith(".Ingame") ?? false)
            .OrderBy(i => i.FullName, StringComparer.Ordinal)
            .ToList();

    void WriteTerminals(List<BlockInfo> blocks, string fileName)
    {
        var document = new XDocument(new XElement("terminals"));
        foreach (var blockInfo in blocks)
            // ReSharper disable once PossibleNullReferenceException
            document.Root.Add(blockInfo.ToXElement());

        document.Save(fileName);
    }
}

public class BlockInfo(
    string typeDefinition,
    string subtypeName,
    List<string> subtypes,
    Type blockType,
    Type declaredInterfaceType,
    List<Type> ingameInterfaces,
    List<ITerminalAction> actions,
    List<ITerminalProperty> properties)
{
    /// <summary>
    ///     Whether an interface descends from this is what tells a consumer it can fetch a block through it.
    ///     Recorded here rather than left to be worked out later, because this is the only place the actual
    ///     type hierarchy is available.
    /// </summary>
    static readonly Type TerminalBlockInterface = typeof(Sandbox.ModAPI.Ingame.IMyTerminalBlock);

    /// <summary>
    ///     The block's object builder type without its prefix, and the key a block is listed under.
    /// </summary>
    public string TypeDefinition { get; } = typeDefinition;

    /// <summary>
    ///     Which subtype was sampled. One definition per type definition is spawned and which one wins depends on
    ///     the order the game returns them in, so this records what the numbers below actually came from.
    /// </summary>
    public string SubtypeName { get; } = subtypeName;

    /// <summary>
    ///     Every subtype sharing this type definition. The actions and properties apply to all of them, not only
    ///     to <see cref="SubtypeName" />.
    /// </summary>
    public ReadOnlyCollection<string> Subtypes { get; } = new(subtypes.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList());

    public Type BlockType { get; } = blockType;

    /// <summary>
    ///     The interface the game declares for this block, if it declares one at all.
    /// </summary>
    public Type DeclaredInterfaceType { get; } = declaredInterfaceType;

    public ReadOnlyCollection<Type> IngameInterfaces { get; } = new(ingameInterfaces);

    public ReadOnlyCollection<ITerminalProperty> Properties { get; } = new(properties);

    public ReadOnlyCollection<ITerminalAction> Actions { get; } = new(actions);

    public XElement ToXElement()
    {
        var root = new XElement("block",
            new XAttribute("typedefinition", TypeDefinition ?? ""),
            new XAttribute("sampledsubtype", SubtypeName ?? ""),
            new XAttribute("class", BlockType.FullName ?? ""),
            new XAttribute("type", DeclaredInterfaceType?.FullName ?? ""));

        foreach (var subtype in Subtypes)
            root.Add(new XElement("subtype", new XAttribute("name", subtype)));
        foreach (var ingameInterface in IngameInterfaces)
            root.Add(new XElement("interface",
                new XAttribute("name", ingameInterface.FullName ?? ""),
                new XAttribute("terminal", TerminalBlockInterface.IsAssignableFrom(ingameInterface) ? "true" : "false")));
        foreach (var action in Actions)
            root.Add(new XElement("action", new XAttribute("name", action.Id), new XAttribute("text", action.Name)));
        foreach (var property in Properties)
            root.Add(new XElement("property", new XAttribute("name", property.Id), new XAttribute("type", property.TypeName)));
        return root;
    }
}

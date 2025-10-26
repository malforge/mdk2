// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Digi.BuildInfo.Features.LiveData;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;
using VRageMath;

namespace Mdk.Extractor;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ExtractorPlugin : IPlugin
{
    const string ObjectBuilderPrefix = "MyObjectBuilder_";

    bool _firstInit = true;

    public SpaceEngineersGame Game { get; private set; }

    public void Dispose() { }

    public void Init(object gameInstance)
    {
        MyLog.Default.Info("Extractor Plugin Loaded.");

        Game = (SpaceEngineersGame)gameInstance;
    }

    public async void Update()
    {
        if (!_firstInit)
            return;

        _firstInit = false;
        await Task.Delay(TimeSpan.FromSeconds(1));
        MySandboxGame.Static.Invoke(() =>
            {
                MySession.AfterLoading += MySession_AfterLoading;
                var screen = MyScreenManager.GetFirstScreenOfType<MyGuiScreenMainMenu>();
                var button = (MyGuiControlButton)screen.Controls.FirstOrDefault(c => c is MyGuiControlButton b && MyTexts.Get(MyCommonTexts.ScreenMenuButtonInventory).EqualsStrFast(b.Text));
                button?.PressButton();
            },
            "Mdk.Extractor");
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

    async void MySession_AfterLoading()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            await GameThread.SwitchToGameThread();
            MySandboxGame.Config.ExperimentalMode = true;
            GrabWhitelist();
            await GrabTerminalAsync();

            await Task.Delay(TimeSpan.FromSeconds(1));
            await GameThread.SwitchToGameThread();
            MySandboxGame.ExitThreadSafe();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    void GrabWhitelist() => WriteWhitelists(Extractor.Current.ModWhitelist, Extractor.Current.PbWhitelist);

    async Task GrabTerminalAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        await GameThread.SwitchToGameThread();
        MySandboxGame.Config.ExperimentalMode = true;
        var result = await SpawnBlocksForAnalysisAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
        await GameThread.SwitchToGameThread();

        if (result != null)
            GrabTerminalActions(Extractor.Current.Terminal, result);
    }

    async Task<List<(MyCubeBlockDefinition, IMyTerminalBlock)>> SpawnBlocksForAnalysisAsync()
    {
        try
        {
            var blocks = new List<(MyCubeBlockDefinition, IMyTerminalBlock)>();
 
            var largeGrid = await SpawnGridAsync(MyCubeSize.Large, new Vector3D(100, 0, 0), MyDefinitionManager.Static.GetAllDefinitions()
                .OfType<MyCubeBlockDefinition>()
                .Where(d => d.CubeSize == MyCubeSize.Large)
                .ToList());
            var fats = largeGrid.GetFatBlocks();
            foreach (var fat in fats)
            {
                if (fat is IMyTerminalBlock block)
                {
                    var def = MyDefinitionManager.Static.GetCubeBlockDefinition(fat.BlockDefinition.Id);
                    blocks.Add((def, block));
                }
            }
            
            var smallGrid = await SpawnGridAsync(MyCubeSize.Small, new Vector3D(0, 100, 0), MyDefinitionManager.Static.GetAllDefinitions()
                .OfType<MyCubeBlockDefinition>()
                .Where(d => d.CubeSize == MyCubeSize.Small)
                .ToList());
            fats = smallGrid.GetFatBlocks();
            foreach (var fat in fats)
            {
                if (fat is IMyTerminalBlock block)
                {
                    var def = MyDefinitionManager.Static.GetCubeBlockDefinition(fat.BlockDefinition.Id);
                    blocks.Add((def, block));
                }
            }
            
            return blocks;
        }
        catch (ReflectionTypeLoadException e)
        {
            foreach (var loaderException in e.LoaderExceptions) MyLog.Default.Error(loaderException.ToString());
            throw;
        }
    }

    async Task<MyCubeGrid> SpawnGridAsync(MyCubeSize size, Vector3D spawnPos, List<MyCubeBlockDefinition> list)
    {
        MyObjectBuilder_CubeGrid gridOb = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CubeGrid>();
        gridOb.EntityId = 0;
        gridOb.DisplayName = "TempGrid";
        gridOb.CreatePhysics = false;
        gridOb.GridSizeEnum = size;
        gridOb.PositionAndOrientation = new MyPositionAndOrientation(spawnPos, Vector3.Forward, Vector3.Up);
        gridOb.PersistentFlags = MyPersistentEntityFlags2.InScene;
        gridOb.IsStatic = true;
        gridOb.Editable = false;
        gridOb.DestructibleBlocks = false;
        gridOb.IsRespawnGrid = false;
        
        var pos = Vector3I.Zero;
        foreach (var def in list)
        {
            var blockOb = (MyObjectBuilder_CubeBlock)MyObjectBuilderSerializer.CreateNewObject(def.Id);
            blockOb.EntityId = 0;
            blockOb.Min = pos;
            pos.X += def.Size.X;
            gridOb.CubeBlocks.Add(blockOb);
        }
        
        var tcs = new TaskCompletionSource<MyCubeGrid>();
        var grid = (MyCubeGrid)MyAPIGateway.Entities.CreateFromObjectBuilderParallel(gridOb, true, e =>
        {
            tcs.SetResult(e as MyCubeGrid);
        });
        grid.IsPreview = true;
        grid.Save = false;
        return await tcs.Task.ConfigureAwait(false);
    }

    void GrabTerminalActions(string terminalFileName, List<(MyCubeBlockDefinition, IMyTerminalBlock)> blocks)
    {
        try
        {
            if (string.IsNullOrEmpty(terminalFileName))
                return;
            MyLog.Default.WriteLineAndConsole("MDK2 Extractor: Extracting terminal actions and properties");
            // var targetsArgumentIndex = commandLine.IndexOf("-terminalcaches");
            // if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
            //     return;
            // var targetsArgument = commandLine[targetsArgumentIndex + 1];
            // var targets = targetsArgument.Split(';');

            var blockInfos = new List<BlockInfo>();
            var totalTime = TimeSpan.Zero;
            var timeSinceLastWrite = TimeSpan.Zero;
            var n = 0;
            var total = blocks.Count;
            foreach (var (cbd, block) in blocks)
            {
                var stopwatch = Stopwatch.StartNew();
                var infoAttribute = block.GetType().GetCustomAttribute<MyTerminalInterfaceAttribute>();
                if (infoAttribute == null)
                {
                    MyLog.Default.Info($"Could not get any info for {cbd.Id} because there's no interface attribute");
                    continue;
                }

                var ingameType = infoAttribute.LinkedTypes.FirstOrDefault(t => t.Namespace?.EndsWith(".Ingame") ?? false);
                if (ingameType == null)
                {
                    MyLog.Default.Info($"Could not get any info for {cbd.Id} because there's no ingame interface in the interface attribute");
                    continue;
                }


                var actions = new List<ITerminalAction>(new List<ITerminalAction>());
                var properties = new List<ITerminalProperty>();
                block.GetActions(actions);
                block.GetProperties(properties);

                MyLog.Default.Info($"Got {actions.Count} actions and {properties.Count} properties from {cbd.Id}");

                var blockInfo = new BlockInfo(block.GetType(), FindTypeDefinition(block.GetType()), ingameType, actions, properties);
                if (blockInfo.BlockInterfaceType != null && blockInfos.All(b => b.BlockInterfaceType != blockInfo.BlockInterfaceType))
                    blockInfos.Add(blockInfo);
                var elapsed = stopwatch.Elapsed;
                totalTime += elapsed;
                var averageTime = totalTime.TotalMilliseconds / ++n;
                var estimatedTime = TimeSpan.FromMilliseconds(averageTime * (total - n));
                timeSinceLastWrite += elapsed;
                if (timeSinceLastWrite > TimeSpan.FromSeconds(1))
                {
                    timeSinceLastWrite = TimeSpan.Zero;
                    Console.WriteLine($@"Estimated time left: {estimatedTime}");
                }
            }
            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: Writing terminal cache {terminalFileName}");
            WriteTerminals(blockInfos, terminalFileName);
        }
        catch (ReflectionTypeLoadException e)
        {
            foreach (var loaderException in e.LoaderExceptions) MyLog.Default.Error(loaderException.ToString());
            throw;
        }
    }

    string FindTypeDefinition(Type block)
    {
        var attr = block.GetCustomAttribute<MyCubeBlockTypeAttribute>();
        if (attr == null)
            return null;
        return attr.ObjectBuilderType.Name.StartsWith(ObjectBuilderPrefix) ? attr.ObjectBuilderType.Name.Substring(ObjectBuilderPrefix.Length) : attr.ObjectBuilderType.Name;
    }

    void WriteTerminals(List<BlockInfo> blocks, string fileName)
    {
        var document = new XDocument(new XElement("terminals"));
        foreach (var blockInfo in blocks)
            // ReSharper disable once PossibleNullReferenceException
            document.Root.Add(blockInfo.ToXElement());

        document.Save(fileName);
    }
}

public class BlockInfo(Type blockType, string typeDefinition, Type blockInterfaceType, List<ITerminalAction> actions, List<ITerminalProperty> properties)
{
    public Type BlockType { get; } = blockType;
    public string TypeDefinition { get; } = typeDefinition;
    public Type BlockInterfaceType { get; } = blockInterfaceType;

    public ReadOnlyCollection<ITerminalProperty> Properties { get; set; } = new(properties);

    public ReadOnlyCollection<ITerminalAction> Actions { get; set; } = new(actions);

    public void Write(TextWriter writer)
    {
        writer.WriteLine(BlockInterfaceType.FullName);
        foreach (var action in Actions)
            writer.WriteLine($"- action {action.Id}");
        foreach (var property in Properties)
            writer.WriteLine($"- action {property.Id} {DetermineType(property.TypeName)}");
    }

    string DetermineType(string propertyTypeName) => propertyTypeName;

    public XElement ToXElement()
    {
        var root = new XElement("block", new XAttribute("type", BlockInterfaceType.FullName ?? ""), new XAttribute("typedefinition", TypeDefinition ?? ""));
        foreach (var action in Actions)
            root.Add(new XElement("action", new XAttribute("name", action.Id), new XAttribute("text", action.Name)));
        foreach (var property in Properties)
            root.Add(new XElement("property", new XAttribute("name", property.Id), new XAttribute("type", DetermineType(property.TypeName))));
        return root;
    }
}
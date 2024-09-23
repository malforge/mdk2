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
using System.Threading.Tasks;
using System.Xml.Linq;
using Digi.BuildInfo.Features.LiveData;
using Sandbox;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using VRage;
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
        await Task.Delay(TimeSpan.FromSeconds(1));
        await GameThread.SwitchToGameThread();
        MySandboxGame.Config.ExperimentalMode = true;
        GrabWhitelist();
        await GrabTerminalAsync();
        
        await Task.Delay(TimeSpan.FromSeconds(1));
        await GameThread.SwitchToGameThread();
        MySandboxGame.ExitThreadSafe();
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
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var position = new Vector3D(100000, 0, 0);
                if (definition is MyCubeBlockDefinition cbd)
                {
                    Debug.WriteLine(definition.Id);
                    await GameThread.SwitchToGameThread();
                    var tcs = new TaskCompletionSource<(MyCubeBlockDefinition, IMyTerminalBlock)>();
                    TempBlockSpawn.Spawn(cbd,
                        false,
                        spawnPos: position,
                        callback: slim =>
                        {
                            if (slim.FatBlock is not IMyTerminalBlock block)
                            {
                                tcs.SetResult((null, null));
                                return;
                            }
                            
                            tcs.SetResult((cbd, block));
                        });
                    var pair = await tcs.Task;
                    position.Z += 100;
                    if (pair.Item1 != null || pair.Item2 != null)
                        blocks.Add(pair);
                    await Task.Delay(100);
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
    
    void GrabTerminalActions(string terminalFileName, List<(MyCubeBlockDefinition, IMyTerminalBlock)> blocks)
    {
        try
        {
            if (string.IsNullOrEmpty(terminalFileName))
                return;
            MyLog.Default.WriteLineAndConsole($"MDK2 Extractor: Extracting terminal actions and properties");
            // var targetsArgumentIndex = commandLine.IndexOf("-terminalcaches");
            // if (targetsArgumentIndex == -1 || targetsArgumentIndex == commandLine.Count - 1)
            //     return;
            // var targetsArgument = commandLine[targetsArgumentIndex + 1];
            // var targets = targetsArgument.Split(';');
            
            var blockInfos = new List<BlockInfo>();
            var totalTime = TimeSpan.Zero;
            TimeSpan timeSinceLastWrite = TimeSpan.Zero;
            int n = 0;
            int total = blocks.Count;
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
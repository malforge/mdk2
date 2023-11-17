// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game;
using SpaceEngineers.Game.GUI;
using VRage;
using VRage.Plugins;
using VRage.Scripting;
using VRage.Utils;

namespace Mdk.Extractor;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ExtractorPlugin : IPlugin
{
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
        List<string> modTypes = string.IsNullOrEmpty(modWhitelist)? null : new();
        List<string> pbTypes = string.IsNullOrEmpty(pbWhitelist)? null : new();

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

        await Task.Delay(TimeSpan.FromSeconds(1));
        await GameThread.SwitchToGameThread();
        MySandboxGame.ExitThreadSafe();
    }

    void GrabWhitelist()
    {
        WriteWhitelists(Extractor.Current.ModWhitelist, Extractor.Current.PbWhitelist);
    }
}
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {

        public static System.Text.RegularExpressions.Regex navRegStr = new System.Text.RegularExpressions.Regex("^(\\{(\\S+)\\}){0,1}(\\[(\\S+)\\]){0,1}(.+)$");
        
        public static Nested1.Nested2 nested = new Nested1.Nested2();

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"Matches? {navRegStr.IsMatch(argument)}");
            
            Echo($"Nested: {nested}");
        }
    }

    public class Nested1
    {
        public class Nested2
        {
            
        }
    }
}
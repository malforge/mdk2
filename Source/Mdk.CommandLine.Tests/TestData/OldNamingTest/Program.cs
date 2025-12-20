using VRage.Game.ModAPI.Ingame.Utilities;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        public Program()
        {
            Echo("OldNamingTest Script Initialized");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"Running with argument: {argument}");
        }
    }
}

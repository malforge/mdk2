using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace OldNamingModTest
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ModSession : MySessionComponentBase
    {
        public override void LoadData()
        {
            MyAPIGateway.Utilities.ShowMessage("OldNamingModTest", "Mod initialized with OLD naming convention");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.ShowMessage("OldNamingModTest", "Mod unloaded");
        }
    }
}

using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace NewNamingModTest
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class ModSession : MySessionComponentBase
    {
        public override void LoadData()
        {
            MyAPIGateway.Utilities.ShowMessage("NewNamingModTest", "Mod initialized with NEW naming convention");
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.ShowMessage("NewNamingModTest", "Mod unloaded");
        }
    }
}

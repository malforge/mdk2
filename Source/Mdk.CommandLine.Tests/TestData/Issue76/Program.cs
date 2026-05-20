using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        public enum Property { Auto }

        public static void AddBooleanHandler(Property property, System.Func<object, bool> getter, System.Action<object, bool> setter)
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
        }
    }
}

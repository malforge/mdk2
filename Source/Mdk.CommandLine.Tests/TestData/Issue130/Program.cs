using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        public Program()
        {
        }

        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"{Beans.Pinto} {Beans.Kidney}");
        }
    }

    #region mdk preserve

    public enum Beans
    {
        Pinto,
        Kidney,
        Red,
    }

    #endregion
}

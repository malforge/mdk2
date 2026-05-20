using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        public void Main(string argument, UpdateType updateSource)
        {
#if THIS_SHOULD_BE_INCLUDED
            IncludedAsUserConstant();
#endif
#if MyRandomConfiguration
            NotIncludedAsBuildName();
#endif
        }

#if THIS_SHOULD_BE_INCLUDED
        void IncludedAsUserConstant() { }
#endif

#if MyRandomConfiguration
        void NotIncludedAsBuildName() { }
#endif
    }
}

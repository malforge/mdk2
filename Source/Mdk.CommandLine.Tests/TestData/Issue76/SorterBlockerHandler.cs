using static IngameScript.Program;

namespace IngameScript
{
    public partial class Program
    {
        public class SorterBlockerHandler
        {
            public SorterBlockerHandler()
            {
                AddBooleanHandler(Property.Auto, b => true, (b, v) => { });
            }
        }
    }
}

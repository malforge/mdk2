using System;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        public Program()
        {
            var instance = new Test();
            var typeName = nameof(Test);
            var propValue = instance.Name;
            var typeofTest = typeof(Test);
        }

        public void Main()
        {
        }

        public class Test : TestBase
        {
            public string Name => nameof(Test);
            
            public void UseTest()
            {
                Test();
                var name = nameof(Test);
                var type = typeof(Test);
            }
        }

        public abstract class TestBase
        {
            protected void Test() { }
        }
    }
}

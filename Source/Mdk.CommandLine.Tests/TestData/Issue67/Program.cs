using Sandbox.ModAPI.Ingame;

// Dummy project to demonstrate rogue enum inclusion in output.
// Project contains three classes. EnhancedBlock is referenced in Program.cs, EnhancedLight is not referenced in Program.cs
// and Utility is not referenced in Program.cs.  As expected, EnhancedBlock is built into the output and EnhancedLight is not.
// However, Utility is built into the output, despite not being referenced.

namespace IngameScript {
    partial class Program : MyGridProgram {
        EnhancedBlock enhancedBlock;

        public Program() {
            enhancedBlock = new EnhancedBlock();
        }

        public void Main(string argument, UpdateType updateSource) {
            Echo(enhancedBlock.someMethod());
        }
    }
}
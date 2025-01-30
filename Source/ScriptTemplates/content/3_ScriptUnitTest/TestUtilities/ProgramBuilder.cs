using System;
using System.Runtime.Serialization;
using FakeItEasy;
using Sandbox.ModAPI.Ingame;

namespace PbScriptTests.TestUtilities
{
    /// <summary>
    /// Provides utility methods for setting up and testing Space Engineers' programmable block scripts.
    /// </summary>
    public static class Gateway
    {
        /// <summary>
        /// Sets the storage value for a given program instance.
        /// </summary>
        /// <typeparam name="T">A type that implements <see cref="Sandbox.ModAPI.IMyGridProgram"/>.</typeparam>
        /// <param name="program">The program instance.</param>
        /// <param name="storage">The storage string to set.</param>
        public static void SetStorage<T>(this T program, string storage)
            where T : Sandbox.ModAPI.IMyGridProgram
        {
            program.Storage = storage;
        }

        /// <summary>
        /// Creates a new <see cref="ProgramBuilder{T}"/> to construct an instance of a Space Engineers script.
        /// </summary>
        /// <typeparam name="T">The script type, which must inherit from <see cref="MyGridProgram"/>.</typeparam>
        /// <returns>A <see cref="ProgramBuilder{T}"/> instance for configuring and constructing the script.</returns>
        /// <remarks>
        /// This method utilizes the builder pattern to allow gradual configuration of the script before instantiation.
        /// The builder ensures all necessary dependencies are set up for proper execution in a test environment.
        /// </remarks>
        /// <example>
        /// The following example demonstrates how to create an instance of a script with specific dependencies:
        /// <code lang="csharp">
        /// var program = Gateway.CreateProgram&lt;MyScript&gt;()
        ///     .WithGridTerminalSystem(myGridSystem)
        ///     .WithRuntime(myRuntime)
        ///     .WithEcho(Console.WriteLine)
        ///     .Build();
        /// </code>
        /// </example>
        public static ProgramBuilder<T> CreateProgram<T>()
            where T : MyGridProgram
        {
            return new ProgramBuilder<T>(null, null, null, null, null, null);
        }

        /// <summary>
        /// A builder for constructing instances of Space Engineers scripts with specific dependencies.
        /// </summary>
        /// <typeparam name="T">The script type, which must inherit from <see cref="MyGridProgram"/>.</typeparam>
        /// <remarks>
        /// The builder pattern is used to allow flexible configuration of dependencies before final instantiation.
        /// Each method (e.g., <see cref="WithGridTerminalSystem"/>, <see cref="WithRuntime"/>) returns a new builder instance
        /// with the specified dependency, ensuring immutability.
        /// 
        /// The `Build` method finalizes the construction, initializing the script with the provided dependencies.
        /// </remarks>
        public readonly struct ProgramBuilder<T> where T : MyGridProgram
        {
            /// <summary>
            /// Builds the final instance of the script with the configured dependencies.
            /// </summary>
            /// <returns>An initialized instance of type <typeparamref name="T"/>.</returns>
            /// <exception cref="InvalidOperationException">Thrown if the script lacks a parameterless constructor or the required interface.</exception>
            public T Build()
            {
                // Create an uninitialized instance (bypasses constructor)
                var program = FormatterServices.GetUninitializedObject(typeof(T));

                // Ensure a parameterless constructor exists
                var constructor = typeof(T).GetConstructor(Type.EmptyTypes);
                if (constructor == null)
                    throw new InvalidOperationException("No parameterless constructor found.");

                if (!(program is Sandbox.ModAPI.IMyGridProgram backend))
                    throw new InvalidOperationException("No IMyGridProgram interface found.");

                // Assign dependencies
                backend.Runtime = GetRuntime();
                backend.Echo = GetEcho();
                backend.Me = GetMe();
                backend.Storage = GetStorage();
                backend.GridTerminalSystem = GetGridTerminalSystem();
                backend.IGC_ContextGetter = GetIgcContextGetter();

                if (!backend.HasMainMethod)
                    throw new InvalidOperationException("No valid Main method found.");

                return (T)program;
            }

            private readonly IMyIntergridCommunicationSystem _igc;
            private readonly IMyGridTerminalSystem _gridTerminalSystem;
            private readonly IMyGridProgramRuntimeInfo _runtime;
            private readonly IMyProgrammableBlock _me;
            private readonly Action<string> _echo;
            private readonly string _storage;

            /// <summary>
            /// Initializes a new builder instance with optional dependencies.
            /// </summary>
            public ProgramBuilder(IMyIntergridCommunicationSystem igc, IMyGridTerminalSystem gridTerminalSystem, IMyGridProgramRuntimeInfo runtime, IMyProgrammableBlock me, Action<string> echo, string storage)
            {
                _igc = igc;
                _gridTerminalSystem = gridTerminalSystem;
                _runtime = runtime;
                _me = me;
                _echo = echo;
                _storage = storage;
            }

            /// <summary>
            /// Specifies a custom Inter-Grid Communication (IGC) system.
            /// </summary>
            public ProgramBuilder<T> WithIgc(IMyIntergridCommunicationSystem igc) =>
                new ProgramBuilder<T>(igc, _gridTerminalSystem, _runtime, _me, _echo, _storage);

            /// <summary>
            /// Specifies a custom Grid Terminal System.
            /// </summary>
            public ProgramBuilder<T> WithGridTerminalSystem(IMyGridTerminalSystem gridTerminalSystem) =>
                new ProgramBuilder<T>(_igc, gridTerminalSystem, _runtime, _me, _echo, _storage);

            /// <summary>
            /// Specifies a custom runtime environment.
            /// </summary>
            public ProgramBuilder<T> WithRuntime(IMyGridProgramRuntimeInfo runtime) =>
                new ProgramBuilder<T>(_igc, _gridTerminalSystem, runtime, _me, _echo, _storage);

            /// <summary>
            /// Specifies a custom programmable block reference.
            /// </summary>
            public ProgramBuilder<T> WithMe(IMyProgrammableBlock me) =>
                new ProgramBuilder<T>(_igc, _gridTerminalSystem, _runtime, me, _echo, _storage);

            /// <summary>
            /// Specifies a custom echo function.
            /// </summary>
            public ProgramBuilder<T> WithEcho(Action<string> echo) =>
                new ProgramBuilder<T>(_igc, _gridTerminalSystem, _runtime, _me, echo, _storage);

            /// <summary>
            /// Specifies custom storage data.
            /// </summary>
            public ProgramBuilder<T> WithStorage(string storage) =>
                new ProgramBuilder<T>(_igc, _gridTerminalSystem, _runtime, _me, _echo, storage);

            private Func<IMyIntergridCommunicationSystem> GetIgcContextGetter()
            {
                var igc = _igc ?? A.Fake<IMyIntergridCommunicationSystem>(o => o.Strict());
                return () => igc;
            }

            private IMyGridTerminalSystem GetGridTerminalSystem() =>
                _gridTerminalSystem ?? A.Fake<IMyGridTerminalSystem>(o => o.Strict());

            private string GetStorage() => _storage ?? string.Empty;

            private IMyProgrammableBlock GetMe() =>
                _me ?? A.Fake<IMyProgrammableBlock>(o => o.Strict());

            private Action<string> GetEcho() => _echo ?? Console.WriteLine;

            private IMyGridProgramRuntimeInfo GetRuntime() =>
                _runtime ?? A.Fake<IMyGridProgramRuntimeInfo>(o => o.Strict());
        }
    }
}

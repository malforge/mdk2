using FakeItEasy;
using IngameScript;
using NUnit.Framework;
using PbScriptTests.TestUtilities;
using Sandbox.ModAPI.Ingame;

namespace PbScriptTests.Tests
{
    /// <summary>
    ///     Sample tests for the <see cref="Program" /> class.
    /// </summary>
    /// <remarks>
    ///     You will need to add a reference to your Space Engineers script project before these tests will pass. You should
    ///     also make sure your Program class is public.
    /// </remarks>
    [TestFixture]
    public class InstancingTests
    {
        /// <summary>
        ///     Happy-case test for creating a new program instance.
        /// </summary>
        /// <remarks>
        ///     Demonstrates the most basic way to create a new program instance.
        ///     You cannot simply instantiate the program class directly, as it inherits from <see cref="MyGridProgram" />
        ///     and requires specific dependencies to be set up, and a specific way to be instantiated.
        /// </remarks>
        [Test]
        public void NewProgram_WhenCalled_ShouldNotThrow()
        {
            // Act
            var program = Gateway.CreateProgram<Program>()
                .Build();

            // Assert
            Assert.That(program, Is.Not.Null);
        }

        /// <summary>
        ///     An example test showing how to swap out the default Echo method with a custom one.
        /// </summary>
        [Test]
        public void NewProgram_WithCustomEcho_RunsCustomEcho()
        {
            // Arrange
            string echoMessage = null;
            var program = Gateway.CreateProgram<Program>()
                .WithEcho(message => echoMessage = message)
                .Build();

            // Act
            program.Echo("Hello, World!");

            // Assert
            Assert.That(echoMessage, Is.EqualTo("Hello, World!"));
        }

        /// <summary>
        ///     An example test showing how to set the storage value for a program instance.
        /// </summary>
        [Test]
        public void NewProgram_WithCustomPbFake_HasMeInstance()
        {
            // Arrange
            var me = A.Fake<IMyProgrammableBlock>();
            A.CallTo(() => me.IsRunning).Returns(true);
            A.CallTo(() => me.CustomName).Returns("Test PB");

            // Act
            var program = Gateway.CreateProgram<Program>()
                .WithMe(me)
                .Build();

            // Assert
            Assert.That(program.Me.IsRunning, Is.True);
        }
    }
}
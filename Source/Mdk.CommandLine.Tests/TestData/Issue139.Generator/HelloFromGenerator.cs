using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Issue139.Generator
{
    [Generator]
    public class HelloFromGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx => ctx.AddSource(
                "HelloFromGenerator.g.cs",
                SourceText.From(
                    "namespace Issue139 { internal static class HelloFromGenerator { public const string Greeting = \"Hello from source generator\"; } }",
                    Encoding.UTF8)));
        }
    }
}

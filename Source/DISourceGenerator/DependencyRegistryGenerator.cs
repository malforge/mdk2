using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DISourceGenerator;

[Generator]
public class DependencyRegistryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Pregenerate the attribute
        context.RegisterPostInitializationOutput(ctx => ctx.AddSource("DependencyRegistryAttribute.g.cs", FrameworkProducer.Instance.Produce()));

        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                static (ctx, _) => Transform(ctx))
            .Where(static x => x.HasValue)
            .SelectMany(static (items, _) => items!.Value);


        context.RegisterSourceOutput(candidates.Collect(),
            static (spc, items) =>
            {
                if (items.Length == 0) return;

                var producer = new RegistryProducer(items);
                spc.AddSource("DependencyRegistry.g.cs", producer.Produce());
            });
    }

    private static ImmutableArray<Item>? Transform(GeneratorSyntaxContext ctx)
    {
        var cds = (ClassDeclarationSyntax)ctx.Node;
        if (ctx.SemanticModel.GetDeclaredSymbol(cds) is not INamedTypeSymbol impl)
            return null;

        var items = ImmutableArray.CreateBuilder<Item>();

        foreach (var attr in impl.GetAttributes())
        {
            if (attr.AttributeClass is { Name: "SingletonAttribute" or "Singleton" or "InstanceAttribute" or "Instance" } ac)
            {
                var isInstance = ac.Name is "InstanceAttribute" or "Instance";
                switch (ac.Arity)
                {
                    case 0:
                        items.Add(new Item(impl, impl, isInstance));
                        break;

                    case 1:
                        var t = ac.TypeArguments[0];
                        items.Add(new Item(impl, t, isInstance));
                        break;

                    case > 1:
                        // Skip invalid attributes
                        break;
                }
            }
        }

        return items.Count > 0 ? items.ToImmutable() : null;
    }

    public readonly struct Item
    {
        public readonly INamedTypeSymbol Implementation;
        public readonly ITypeSymbol Service;
        public readonly bool IsInstance;

        public Item(INamedTypeSymbol implementation, ITypeSymbol service, bool isInstance)
        {
            Implementation = implementation;
            Service = service;
            IsInstance = isInstance;
        }
    }
}
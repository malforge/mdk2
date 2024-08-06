using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Mal.DocumentGenerator.Whitelists;
using Mono.Cecil;

namespace Mal.DocumentGenerator.Dom;

public class DocDomBuilder(Whitelist whitelist)
{
    readonly Dictionary<string, DocTypeBuilder> _types = new();
    public Whitelist Whitelist { get; } = whitelist;

    public DocTypeBuilder GetOrAddType(TypeReference typeReference)
    {
        var typeDefinition = typeReference.Resolve();
        var fullName = typeDefinition.GetCSharpName();
        if (_types.TryGetValue(fullName, out var builder))
            return builder;

        builder = typeDefinition.IsEnum ? new DocEnumBuilder(this) :
            typeDefinition.IsDelegate() ? new DocDelegateBuilder(this) :
            new DocComplexTypeBuilder(this, typeDefinition);
        _types.Add(fullName, builder);

        var parent = typeReference.DeclaringType;
        if (parent != null)
        {
            var parentType = (DocComplexTypeBuilder)GetOrAddType(parent);
            parentType.WithNestedType(builder);
        }

        builder.Visit();

        return builder;
    }

    public IDocDom Build() =>
        new DocDom(_types.Values.Select(t => t.Build()).ToImmutableArray());

    class DocDom : IDocDom
    {
        public DocDom(ImmutableArray<IDocType> types)
        {
            Types = types;
        }

        public ImmutableArray<IDocType> Types { get; }
    }
}
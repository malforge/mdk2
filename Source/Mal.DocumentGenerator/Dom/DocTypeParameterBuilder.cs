using System.Collections.Immutable;
using System.Linq;
using Mono.Cecil;

namespace Mal.DocumentGenerator.Dom;

public class DocTypeParameterBuilder(DocDomBuilder context, GenericParameter parameter) : DocElementBuilder(context)
{
    readonly Mono.Cecil.GenericParameter _parameter = parameter;
    bool _isVisited;
    public string Name => _parameter.Name;

    public DocTypeParameterBuilder Visit()
    {
        if (_isVisited)
            return this;
        _isVisited = true;
        OnVisit();
        return this;
    }

    public IDocGenericParameter Build()
    {
        var mustBeClass = _parameter.HasReferenceTypeConstraint;
        var mustBeStruct = _parameter.HasNotNullableValueTypeConstraint;
        var mustHaveDefaultConstructor = _parameter.HasDefaultConstructorConstraint;
        var typeConstraints = _parameter.Constraints.Select(c => Context.GetOrAddType(c.ConstraintType)).Select(t => t.Build()).ToImmutableArray();

        return new GenericParameter(_parameter.Name)
        {
            MustBeClass = mustBeClass,
            MustBeStruct = mustBeStruct,
            MustHaveDefaultConstructor = mustHaveDefaultConstructor,
            TypeConstraints = typeConstraints
        };
    }

    protected virtual void OnVisit() { }

    class GenericParameter(string name) : IDocGenericParameter
    {
        public string Name { get; } = name;
        public bool MustBeClass { get; init; }
        public bool MustBeStruct { get; init; }
        public bool MustHaveDefaultConstructor { get; init; }
        public ImmutableArray<IDocType> TypeConstraints { get; init; }
    }
}
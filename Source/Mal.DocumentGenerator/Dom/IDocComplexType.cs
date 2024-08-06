using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public interface IDocComplexType : IDocType
{
    IDocType? BaseType { get; }
    ImmutableArray<IDocType> Interfaces { get; }
    ImmutableArray<IDocTypeMember> Members { get; }
    ImmutableArray<IDocType> NestedTypes { get; }
    bool IsGeneric => TypeParameters.Length > 0; 
    ImmutableArray<IDocGenericParameter> TypeParameters { get; }
}
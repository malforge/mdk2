using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public interface IDocDelegate : IDocSimpleType
{
    bool IsGeneric => TypeParameters.Length > 0;
    ImmutableArray<IDocGenericParameter> TypeParameters { get; }
}
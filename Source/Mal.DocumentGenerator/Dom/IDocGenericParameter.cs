using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public interface IDocGenericParameter
{
    string Name { get; }
    bool MustBeClass { get; }
    bool MustBeStruct { get; }
    bool MustHaveDefaultConstructor { get; }
    ImmutableArray<IDocType> TypeConstraints { get; }
}
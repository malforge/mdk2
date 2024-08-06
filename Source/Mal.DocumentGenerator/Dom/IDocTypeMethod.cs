using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public interface IDocTypeMethod : IDocTypeMember
{
    IDocType ReturnType { get; }
    bool IsAbstract { get; }
    bool IsVirtual { get; }
    bool IsAsync { get; }
    bool IsGeneric { get; }
    ImmutableArray<IDocParameter> Parameters { get; }
    ImmutableArray<IDocGenericParameter> TypeParameters { get; }
}
using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public interface IDocTypeConstructor : IDocTypeMember
{
    ImmutableArray<IDocParameter> Parameters { get; }
}
using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public interface IDocTypeProperty : IDocTypeMember
{
    IDocType PropertyType { get; }
    bool CanGet { get; }
    bool CanSet { get; }
    bool IsIndexer { get; }
    ImmutableArray<IDocParameter> Indexes { get; }
}
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Mal.DocumentGenerator.Dom;

public interface IDocDom
{
    ImmutableArray<IDocType> Types { get; }

    IEnumerable<IDocTypeElement> Everything() =>
        Types.SelectMany(t => t.Everything());
}
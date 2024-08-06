using System.Collections.Generic;

namespace Mal.DocumentGenerator.Dom;

public interface IDocType : IDocTypeElement
{
    DocTypeKind Kind { get; }
    string FullName { get; }
    IDocType? DeclaringType { get; }
    bool IsNested => DeclaringType != null;
    IEnumerable<IDocTypeElement> Everything();
}
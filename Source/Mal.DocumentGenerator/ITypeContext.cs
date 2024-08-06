using System.Collections.Generic;

namespace Mal.DocumentGenerator;

public interface ITypeContext
{
    XmlDoc XmlDoc { get; }
    IEnumerable<INode> Everything();
    IEnumerable<TypeNode> Types();
    TypeNode? FindType(string className);
}
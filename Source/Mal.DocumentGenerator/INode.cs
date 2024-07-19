using System.Collections.Generic;

namespace Mal.DocumentGenerator;

public interface INode
{
    /// <summary>
    ///     A key used to identify this particular node uniquely, in order to pair separately registered
    ///     information with the same node.
    /// </summary>
    string Key { get; }
    
    /// <summary>
    /// The plain name of the assembly this node belongs to.
    /// </summary>
    string Assembly { get; }

    Node? Parent { get; }
    IEnumerable<Node> Children { get; }
}
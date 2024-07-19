using System.Collections.Generic;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class Context(params string[] directories) : INode, ITypeContainer
{
    readonly Dictionary<string, TypeNode> _types = new();

    public string Key { get; } = "root";
    string INode.Assembly => string.Empty;
    Node? INode.Parent => null;

    public XmlDoc XmlDoc { get; } = new(directories);

    IEnumerable<Node> INode.Children => _types.Values;

    public TypeNode GetOrAddType(TypeDefinition type)
    {
        if (_types.TryGetValue(type.Name, out var node))
            return node;
        node = new TypeNode(this, type);
        _types.Add(type.Name, node);
        return node;
    }

    public IEnumerable<INode> Everything()
    {
        Stack<INode> stack = new();
        foreach (var node in _types.Values)
            stack.Push(node);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            foreach (var child in node.Children)
                stack.Push(child);
        }
    }
}
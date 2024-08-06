using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class TypeContextBuilder(params string[] directories) : INode, ITypeContainer, ITypeContext
{
    readonly Dictionary<string, TypeNode> _types = new();

    bool _isClosed;

    public XmlDoc XmlDoc { get; } = new(directories);

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

    public IEnumerable<TypeNode> Types() => Everything().OfType<TypeNode>();
    public TypeNode? FindType(string key)
    {
        return Types().FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public string Key { get; } = "root";
    string INode.Assembly => string.Empty;
    Node? INode.Parent => null;

    IEnumerable<Node> INode.Children => _types.Values;

    public TypeNode GetOrAddType(TypeDefinition type)
    {
        if (_isClosed)
            throw new InvalidOperationException("ContextBuilder is closed.");
        
        if (_types.TryGetValue(type.Name, out var node))
            return node;
        node = new TypeNode(this, type);
        _types.Add(type.Name, node);
        return node;
    }

    public void Close() => _isClosed = true;

    public string CSharpify(TypeReference type)
    {
        switch (type.FullName)
        {
            case "System.Void": return "void";
            case "System.Boolean": return "bool";
            case "System.Byte": return "byte";
            case "System.SByte": return "sbyte";
            case "System.Int16": return "short";
            case "System.UInt16": return "ushort";
            case "System.Int32": return "int";
            case "System.UInt32": return "uint";
            case "System.Int64": return "long";
            case "System.UInt64": return "ulong";
            case "System.Single": return "single";
            case "System.Double": return "double";
            case "System.String": return "string";
        }

        if (type.HasGenericParameters)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(type.Name);
            sb.Append('<');
            for (int i = 0; i < type.GenericParameters.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(type.GenericParameters[i].Name);
            }
            sb.Append('>');
            return sb.ToString();
        }

        if (type.IsGenericInstance)
        {
            var generic = (GenericInstanceType)type;
            var sb = new System.Text.StringBuilder();
            var tickIndex = type.Name.IndexOf('`');
            if (tickIndex < 0)
                sb.Append(type.Name);
            else
            {
                var baseName = type.Name.Substring(0);
                sb.Append(baseName);
                sb.Append('<');
                for (int i = 0; i < generic.GenericArguments.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(CSharpify(generic.GenericArguments[i]));
                }
                sb.Append('>');
            }
            return sb.ToString();
        }
        
        if (type.IsArray)
        {
            var array = (ArrayType)type;
            return $"{CSharpify(array.ElementType)}[]";
        }
        
        return type.Name;
    }
}
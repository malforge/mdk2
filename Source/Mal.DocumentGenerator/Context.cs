using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class Context
{
    readonly Dictionary<string, AssemblyNode> _assemblies = new();

    public AssemblyNode GetOrAddAssembly(AssemblyDefinition assembly)
    {
        if (_assemblies.TryGetValue(assembly.Name.Name, out var node))
            return node;
        node = new AssemblyNode(assembly.Name.Name);
        _assemblies.Add(assembly.Name.Name, node);
        return node;
    }

    public IEnumerable<INode> Everything()
    {
        Stack<INode> stack = new();
        foreach (var assembly in _assemblies.Values)
        {
            stack.Push(assembly);
        }
        
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }
    }
}

public interface INode
{
    Node? Parent { get; }
    IEnumerable<Node> Children { get; }
}

public interface ITypeContainer
{
    TypeNode GetOrAddType(TypeDefinition type);
}

public abstract class Node : INode
{
    Node? INode.Parent => GetParent();

    IEnumerable<Node> INode.Children => EnumerateChildren();
    protected abstract Node? GetParent();
    protected abstract IEnumerable<Node> EnumerateChildren();
}

public class AssemblyNode(string name) : Node
{
    readonly Dictionary<string, NamespaceNode> _namespaces = new();
    public string Name { get; } = name;

    public NamespaceNode GetOrAddNamespace(string namespaceName)
    {
        if (_namespaces.TryGetValue(namespaceName, out var node))
            return node;
        node = new NamespaceNode(this, namespaceName);
        _namespaces.Add(namespaceName, node);
        return node;
    }

    protected override Node? GetParent() => null;

    protected override IEnumerable<Node> EnumerateChildren() => _namespaces.Values;
}

public class NamespaceNode(AssemblyNode parent, string name) : Node, ITypeContainer
{
    readonly Dictionary<string, TypeNode> _namespaces = new();
    public AssemblyNode Parent { get; } = parent;
    public string Name { get; } = name;
    protected override Node GetParent() => Parent;
    protected override IEnumerable<Node> EnumerateChildren() => _namespaces.Values;

    public TypeNode GetOrAddType(TypeDefinition type)
    {
        if (_namespaces.TryGetValue(type.Name, out var node))
            return node;
        node = new TypeNode(this, type);
        _namespaces.Add(type.Name, node);
        return node;
    }
}

public class TypeNode : Node, ITypeContainer
{
    readonly Dictionary<MethodDefinition, MethodNode> _constructors = new();
    readonly Dictionary<FieldDefinition, FieldNode> _fields = new();
    readonly Dictionary<PropertyDefinition, PropertyNode> _properties = new();
    readonly Dictionary<MethodDefinition, MethodNode> _methods = new();
    readonly Dictionary<EventDefinition, EventNode> _events = new();
    readonly Dictionary<MethodDefinition, MethodNode> _extensionMethods = new();
    readonly Dictionary<TypeDefinition, TypeNode> _nestedTypes = new();

    public TypeNode(NamespaceNode parent, TypeDefinition type)
    {
        Namespace = parent;
        Type = type;
    }
    
    public TypeNode(TypeNode parent, TypeDefinition type)
    {
        ParentType = parent;
        Type = type;
    }

    public NamespaceNode? Namespace { get; }
    public TypeNode? ParentType { get; }
    public TypeDefinition Type { get; }
    protected override Node GetParent() => ((Node?)ParentType ?? (Node?)Namespace) ?? throw new InvalidOperationException("Parent is null");
    protected override IEnumerable<Node> EnumerateChildren() => _constructors.Values
        .Concat<Node>(_fields.Values)
        .Concat(_properties.Values)
        .Concat(_methods.Values)
        .Concat(_events.Values)
        .Concat(_extensionMethods.Values)
        .Concat(_nestedTypes.Values);

    public MethodNode GetOrAddConstructor(MethodDefinition constructor)
    {
        if (_constructors.TryGetValue(constructor, out var node))
            return node;
        node = new MethodNode(this, constructor);
        _constructors.Add(constructor, node);
        return node;
    }
    
    public FieldNode GetOrAddField(FieldDefinition field)
    {
        if (_fields.TryGetValue(field, out var node))
            return node;
        node = new FieldNode(this, field);
        _fields.Add(field, node);
        return node;
    }
    
    public PropertyNode GetOrAddProperty(PropertyDefinition property)
    {
        if (_properties.TryGetValue(property, out var node))
            return node;
        node = new PropertyNode(this, property);
        _properties.Add(property, node);
        return node;
    }
    
    public MethodNode GetOrAddMethod(MethodDefinition method)
    {
        if (_methods.TryGetValue(method, out var node))
            return node;
        node = new MethodNode(this, method);
        _methods.Add(method, node);
        return node;
    }
    
    public EventNode GetOrAddEvent(EventDefinition @event)
    {
        if (_events.TryGetValue(@event, out var node))
            return node;
        node = new EventNode(this, @event);
        _events.Add(@event, node);
        return node;
    }
    
    public MethodNode GetOrAddExtensionMethod(MethodDefinition method)
    {
        if (_extensionMethods.TryGetValue(method, out var node))
            return node;
        node = new MethodNode(this, method);
        _extensionMethods.Add(method, node);
        return node;
    }

    public TypeNode GetOrAddNestedType(TypeDefinition typeDefinition)
    {
        if (_nestedTypes.TryGetValue(typeDefinition, out var node))
            return node;
        node = new TypeNode(this, typeDefinition);
        _nestedTypes.Add(typeDefinition, node);
        return node;
    }

    TypeNode ITypeContainer.GetOrAddType(TypeDefinition type) => GetOrAddNestedType(type);
}

public class MethodNode : Node
{
    public MethodNode(TypeNode parent, MethodDefinition method)
    {
        ParentType = parent;
        Method = method;
    }

    public TypeNode ParentType { get; }
    public MethodDefinition Method { get; }
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}

public class FieldNode : Node
{
    public FieldNode(TypeNode parent, FieldDefinition field)
    {
        ParentType = parent;
        Field = field;
    }

    public TypeNode ParentType { get; }
    public FieldDefinition Field { get; }
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}

public class PropertyNode : Node
{
    public PropertyNode(TypeNode parent, PropertyDefinition property)
    {
        ParentType = parent;
        Property = property;
    }

    public TypeNode ParentType { get; }
    public PropertyDefinition Property { get; }
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}

public class EventNode : Node
{
    public EventNode(TypeNode parent, EventDefinition @event)
    {
        ParentType = parent;
        Event = @event;
    }

    public TypeNode ParentType { get; }
    public EventDefinition Event { get; }
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}


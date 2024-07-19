using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class TypeNode : Node, ITypeContainer
{
    readonly Dictionary<MethodDefinition, MethodNode> _constructors = new();
    readonly Dictionary<EventDefinition, EventNode> _events = new();
    readonly Dictionary<MethodDefinition, MethodNode> _extensionMethods = new();
    readonly Dictionary<FieldDefinition, FieldNode> _fields = new();
    readonly Dictionary<MethodDefinition, MethodNode> _methods = new();
    readonly Dictionary<TypeDefinition, TypeNode> _nestedTypes = new();
    readonly Dictionary<PropertyDefinition, PropertyNode> _properties = new();

    public TypeNode(Context context, TypeDefinition type)
        : base(context, type.GetDocumentationCommentName(), type.Module.Assembly.Name.Name)
    {
        Type = type;
    }

    public TypeNode(TypeNode parent, TypeDefinition type)
        : base(parent.Context, type.GetDocumentationCommentName(), type.Module.Assembly.Name.Name)
    {
        ParentType = parent;
        Type = type;
    }

    public TypeNode? ParentType { get; }
    public TypeDefinition Type { get; }

    TypeNode ITypeContainer.GetOrAddType(TypeDefinition type) => GetOrAddNestedType(type);
    protected override Node? GetParent() => ParentType;

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
        node = new MethodNode(Context, this, constructor);
        _constructors.Add(constructor, node);
        return node;
    }

    public FieldNode GetOrAddField(FieldDefinition field)
    {
        if (_fields.TryGetValue(field, out var node))
            return node;
        node = new FieldNode(Context, this, field);
        _fields.Add(field, node);
        return node;
    }

    public PropertyNode GetOrAddProperty(PropertyDefinition property)
    {
        if (_properties.TryGetValue(property, out var node))
            return node;
        node = new PropertyNode(Context, this, property);
        _properties.Add(property, node);
        return node;
    }

    public MethodNode GetOrAddMethod(MethodDefinition method)
    {
        if (_methods.TryGetValue(method, out var node))
            return node;
        node = new MethodNode(Context, this, method);
        _methods.Add(method, node);
        return node;
    }

    public EventNode GetOrAddEvent(EventDefinition @event)
    {
        if (_events.TryGetValue(@event, out var node))
            return node;
        node = new EventNode(Context, this, @event);
        _events.Add(@event, node);
        return node;
    }

    public MethodNode GetOrAddExtensionMethod(MethodDefinition method)
    {
        if (_extensionMethods.TryGetValue(method, out var node))
            return node;
        node = new MethodNode(Context, this, method);
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
}
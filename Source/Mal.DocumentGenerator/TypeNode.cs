using System;
using System.Collections.Generic;
using System.Linq;
using Mal.DocumentGenerator.Common;
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
    string? _url;

    public TypeNode(TypeContextBuilder context, TypeDefinition type)
        : base(context, type.GetDocumentationCommentName(), type.Module.Assembly.Name.Name)
    {
        TypeDefinition = type;
        Type = new DataType(type);
    }

    public TypeNode(TypeNode parent, TypeDefinition type)
        : base(parent.Context, type.GetDocumentationCommentName(), type.Module.Assembly.Name.Name)
    {
        ParentType = parent;
        TypeDefinition = type;
        Type = new DataType(type);
    }

    public TypeNode? ParentType { get; }
    public TypeDefinition TypeDefinition { get; }
    public DataType Type { get; }

    TypeNode ITypeContainer.GetOrAddType(TypeDefinition type) => GetOrAddNestedType(type);
    protected override Node? GetParent() => ParentType;

    protected override IEnumerable<Node> EnumerateChildren() => _constructors.Values
        .Concat<Node>(_fields.Values)
        .Concat(_properties.Values)
        .Concat(_methods.Values)
        .Concat(_events.Values)
        .Concat(_extensionMethods.Values)
        .Concat(_nestedTypes.Values);

    public override string Signature()
    {
        return Type.Name;
    }

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

    public MethodNode AddExtensionMethod(MethodNode method)
    {
        _extensionMethods.Add(method.Method, method);
        return method;
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

    // public MethodNode GetOrAddExtensionMethod(MethodDefinition method)
    // {
    //     if (_extensionMethods.TryGetValue(method, out var node))
    //         return node;
    //     node = new MethodNode(Context, this, method);
    //     _extensionMethods.Add(method, node);
    //     return node;
    // }

    public TypeNode GetOrAddNestedType(TypeDefinition typeDefinition)
    {
        if (_nestedTypes.TryGetValue(typeDefinition, out var node))
            return node;
        node = new TypeNode(this, typeDefinition);
        _nestedTypes.Add(typeDefinition, node);
        return node;
    }

    public IEnumerable<MethodNode> Constructors() => _constructors.Values;
    public IEnumerable<EventNode> Events(bool includeInherited = true)
    {
        if (!includeInherited)
            return _events.Values;
        
        IEnumerable<EventNode> src = _events.Values;
        
        if (TypeDefinition.IsInterface)
        {
            var interfaces = TypeDefinition.Interfaces.Select(i => Context.Types().First(t => t.TypeDefinition == i.InterfaceType.Resolve()));
            src = _events.Values.Concat(interfaces.SelectMany(i => i.Events()));
        }
        
        if (ParentType != null)
            src = src.Concat(ParentType.Events());
        
        return src;
    }

    public IEnumerable<FieldNode> Fields(bool includeInherited = true)
    {
        if (!includeInherited)
            return _fields.Values;

        IEnumerable<FieldNode> src = _fields.Values;
        
        if (TypeDefinition.IsInterface)
        {
            var interfaces = TypeDefinition.Interfaces.Select(i => Context.Types().First(t => t.TypeDefinition == i.InterfaceType.Resolve()));
            src = _fields.Values.Concat(interfaces.SelectMany(i => i.Fields()));
        }
        
        if (ParentType != null)
            src = src.Concat(ParentType.Fields());
        
        return src;
    }

    public IEnumerable<PropertyNode> Properties(bool includeInherited = true)
    {
        if (!includeInherited)
            return _properties.Values;
        
        IEnumerable<PropertyNode> src = _properties.Values;
        
        if (TypeDefinition.IsInterface)
        {
            var interfaces = TypeDefinition.Interfaces.Select(i => Context.Types().First(t => t.TypeDefinition == i.InterfaceType.Resolve()));
            src = _properties.Values.Concat(interfaces.SelectMany(i => i.Properties()));
        }
        
        if (ParentType != null)
            src = src.Concat(ParentType.Properties());
        
        return src;
    }

    public IEnumerable<MethodNode> Methods(bool includeInherited = true)
    {
        if (!includeInherited)
            return _methods.Values;

        IEnumerable<MethodNode> src = _methods.Values;
        
        if (TypeDefinition.IsInterface && TypeDefinition.Interfaces.Count > 0)
        {
            var interfaces = TypeDefinition.Interfaces.Select(i => Context.Types().First(t => t.Key == i.InterfaceType.Resolve().GetDocumentationCommentName()));
            src = _methods.Values.Concat(interfaces.SelectMany(i => i.Methods()));
        }
        
        if (ParentType != null)
            src = src.Concat(ParentType.Methods());
        
        return src;
    }

    public string Url()
    {
        if (_url == null)
        {
            _url =
                string.Join("/",
                    Type.FullName.Split('.', StringSplitOptions.RemoveEmptyEntries).Select(FileName.GenerateSafeFileName)
                )
                + ".html";
        }
        return _url;
    }
}
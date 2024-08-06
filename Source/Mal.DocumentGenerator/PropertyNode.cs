using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class PropertyNode(ITypeContext context, TypeNode parent, PropertyDefinition property) : Node(context, property.GetDocumentationCommentName(), property.DeclaringType.Module.Assembly.Name.Name)
{
    string? _signature;
    public TypeNode ParentType { get; } = parent;
    public PropertyDefinition Property { get; } = property;
    public string Name => Property.Name;
    public bool IsStatic => Property.GetMethod?.IsStatic ?? Property.SetMethod?.IsStatic ?? false;
    public DataType PropertyType { get; } = new(property.PropertyType);
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];

    public override string Signature()
    {
        if (_signature == null)
        {
            var sb = new StringBuilder();
            if (IsStatic) sb.Append("static ");
            sb.Append(PropertyType.Name);
            sb.Append(' ');
            sb.Append(Property.Name);
            _signature = sb.ToString();
        }
        return _signature;
    }
}
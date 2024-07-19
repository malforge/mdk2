using System.Collections.Generic;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class PropertyNode(Context context, TypeNode parent, PropertyDefinition property) : Node(context, property.GetDocumentationCommentName(), property.DeclaringType.Module.Assembly.Name.Name)
{
    public TypeNode ParentType { get; } = parent;
    public PropertyDefinition Property { get; } = property;
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}
using System.Collections.Generic;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class FieldNode(Context context, TypeNode parent, FieldDefinition field) : Node(context, field.GetDocumentationCommentName(), field.DeclaringType.Module.Assembly.Name.Name)
{
    public TypeNode ParentType { get; } = parent;
    public FieldDefinition Field { get; } = field;
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}
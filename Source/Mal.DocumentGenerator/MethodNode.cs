using System.Collections.Generic;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class MethodNode(Context context, TypeNode parent, MethodDefinition method) : Node(context, method.GetDocumentationCommentName(), method.DeclaringType.Module.Assembly.Name.Name)
{
    public TypeNode ParentType { get; } = parent;
    public MethodDefinition Method { get; } = method;
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}
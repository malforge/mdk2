using System.Collections.Generic;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class EventNode(Context context, TypeNode parent, EventDefinition @event) : Node(context, @event.GetDocumentationCommentName(), @event.DeclaringType.Module.Assembly.Name.Name)
{
    public TypeNode ParentType { get; } = parent;
    public EventDefinition Event { get; } = @event;
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
}
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class EventNode(ITypeContext context, TypeNode parent, EventDefinition @event) : Node(context, @event.GetDocumentationCommentName(), @event.DeclaringType.Module.Assembly.Name.Name)
{
    string? _signature;
    public TypeNode ParentType { get; } = parent;
    public EventDefinition Event { get; } = @event;
    public string Name => Event.Name;

    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];

    public override string Signature()
    {
        if (_signature == null)
        {
           var sb = new StringBuilder();
            sb.Append(Event.Name);
            _signature = sb.ToString();
        }
        return _signature;
    }
}
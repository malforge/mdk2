using System;
using Mono.Cecil;

namespace Mal.DocumentGenerator.Dom;

public class DocTypeEventBuilder(DocDomBuilder context, EventDefinition @event) : DocTypeMemberBuilder(context)
{
    EventDefinition _event = @event;

    public override string Id { get; } = @event.GetCSharpName();
    protected override void OnVisit() => throw new NotImplementedException();
}
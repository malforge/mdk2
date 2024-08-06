using System;
using Mono.Cecil;

namespace Mal.DocumentGenerator.Dom;

public class DocTypePropertyBuilder(DocDomBuilder context, PropertyDefinition property) : DocTypeMemberBuilder(context)
{
    PropertyDefinition _property = property;

    public override string Id { get; } = property.GetCSharpName();
    protected override void OnVisit() => throw new NotImplementedException();
}
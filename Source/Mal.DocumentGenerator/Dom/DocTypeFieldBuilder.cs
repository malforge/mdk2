using System;
using Mono.Cecil;

namespace Mal.DocumentGenerator.Dom;

public class DocTypeFieldBuilder(DocDomBuilder context, FieldDefinition field) : DocTypeMemberBuilder(context)
{
    FieldDefinition _field = field;

    public override string Id { get; } = field.GetCSharpName();
    protected override void OnVisit() => throw new NotImplementedException();
}
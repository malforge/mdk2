using System;
using Mono.Cecil;

namespace Mal.DocumentGenerator.Dom;

public class DocTypeMethodBuilder(DocDomBuilder context, MethodDefinition method, DocTypeBuilder? extensionTarget) : DocTypeMemberBuilder(context)
{
    MethodDefinition _method = method;
    readonly DocTypeBuilder? _extensionTarget = extensionTarget;

    public override string Id { get; } = method.GetCSharpName();
    protected override void OnVisit() => throw new NotImplementedException();
}
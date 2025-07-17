using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class EventDocumentation(TypeDocumentation type, EventDefinition eventDef, DocMember? documentation, string? obsoleteMessage = null)
    : MemberDocumentation(type, eventDef, documentation, obsoleteMessage)
{
    public EventDefinition Event { get; } = eventDef;
    public override sealed string Title { get; } = $"{eventDef.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {eventDef.EventType.GetMemberTypeName()}{(eventDef.IsObsolete() ? " (Obsolete)" : "")}";
    public override sealed string ShortSignature() => Event.Name;
    public override bool IsPublic() => Event.AddMethod.IsPublic || Event.RemoveMethod.IsPublic;
    public override bool IsExternal() => false;
}
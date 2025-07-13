using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class EventDocumentation(EventDefinition eventDef, DocMember? documentation)
    : IMemberDocumentation
{
    public EventDefinition Event { get; } = eventDef;
    MemberReference IMemberDocumentation.Member => Event;
    public DocMember? Documentation { get; } = documentation;
    public string WhitelistKey { get; } = Whitelist.GetEventKey(eventDef);
    public string DocKey { get; } = Doc.GetDocKey(eventDef);
    public string FullName { get; } = eventDef.GetCSharpName();
    public string AssemblyName { get; } = eventDef.Module.Assembly.Name.Name;
    public string Namespace { get; } = eventDef.DeclaringType.Namespace;
    public string Title { get; } = $"{eventDef.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {eventDef.EventType.GetMemberTypeName()}{(eventDef.IsObsolete()? " (Obsolete)" : "")}";
    public string Name { get; } = eventDef.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    public string ShortSignature() => Event.Name;
    public bool IsPublic => Event.AddMethod.IsPublic || Event.RemoveMethod.IsPublic;
}
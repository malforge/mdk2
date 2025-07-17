using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class FieldDocumentation(TypeDocumentation type, FieldDefinition field, DocMember? documentation, string? obsoleteMessage = null)
    : MemberDocumentation(type, field, documentation, obsoleteMessage)
{
    public FieldDefinition Field { get; } = field;
    public override sealed string Title { get; } = $"{field.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {field.FieldType.GetMemberTypeName()}{(field.IsObsolete() ? " (Obsolete)" : "")}";
    public override sealed string ShortSignature() => Field.Name;
    public override bool IsPublic() => Field.IsPublic;
    public override bool IsExternal() => false;
}
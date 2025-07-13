using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class FieldDocumentation(FieldDefinition field, DocMember? documentation, string? obsoleteMessage = null)
    : MemberDocumentation(field, documentation, obsoleteMessage)
{
    public FieldDefinition Field { get; } = field;
    public override sealed string Title { get; } = $"{field.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {field.FieldType.GetMemberTypeName()}{(field.IsObsolete() ? " (Obsolete)" : "")}";
    public override sealed bool IsPublic => Field.IsPublic;
    public override sealed string ShortSignature() => Field.Name;
}
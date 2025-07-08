using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class FieldDocumentation(FieldDefinition field, DocMember? documentation)
    : IMemberDocumentation
{
    public FieldDefinition Field { get; } = field;
    IMemberDefinition IMemberDocumentation.Member => Field;
    public DocMember? Documentation { get; } = documentation;
    public string WhitelistKey { get; } = Whitelist.GetFieldKey(field);
    public string DocKey { get; } = Doc.GetDocKey(field);
    public string FullName { get; } = field.GetCSharpName();
    public string AssemblyName { get; } = field.Module.Assembly.Name.Name;
    public string Namespace { get; } = field.DeclaringType.Namespace;
    public string Title { get; } = $"{field.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {field.FieldType.GetMemberTypeName()}{(field.IsObsolete()? " (Obsolete)" : "")}";
    public string ShortSignature() => Field.Name;

    public bool IsPublic => Field.IsPublic;
}
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public abstract class MemberDocumentation : IMemberDocumentation
{
    readonly MemberReference _member;

    protected MemberDocumentation(MemberReference memberReference, DocMember? docMember, string? obsoleteMessage = null)
    {
        _member = memberReference ?? throw new ArgumentNullException(nameof(memberReference));
        Documentation = docMember;
        WhitelistKey = Whitelist.GetKey(memberReference);
        DocKey = Doc.GetDocKey(memberReference);
        FullName = memberReference.GetCSharpName();
        AssemblyName = memberReference?.Module.Assembly.Name.Name ?? string.Empty;
        Namespace = memberReference is TypeReference typeReference ? typeReference.Namespace : memberReference?.DeclaringType?.Namespace ?? string.Empty;
        Name = memberReference?.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics) ?? string.Empty;
    }

    MemberReference IMemberDocumentation.Member => _member;

    public DocMember? Documentation { get; }
    public string WhitelistKey { get; }
    public string DocKey { get; }
    public string FullName { get; }
    public string AssemblyName { get; }
    public string Namespace { get; }
    public abstract string Title { get; }
    public abstract string Name { get; }
    public abstract string ShortSignature();
}

public class FieldDocumentation(FieldDefinition field, DocMember? documentation)
    : IMemberDocumentation
{
    public FieldDefinition Field { get; } = field;
    MemberReference IMemberDocumentation.Member => Field;
    public DocMember? Documentation { get; } = documentation;
    public string WhitelistKey { get; } = Whitelist.GetFieldKey(field);
    public string DocKey { get; } = Doc.GetDocKey(field);
    public string FullName { get; } = field.GetCSharpName();
    public string AssemblyName { get; } = field.Module.Assembly.Name.Name;
    public string Namespace { get; } = field.DeclaringType.Namespace;
    public string Title { get; } = $"{field.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {field.FieldType.GetMemberTypeName()}{(field.IsObsolete()? " (Obsolete)" : "")}";
    public string Name { get; } = field.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    public string ShortSignature() => Field.Name;

    public bool IsPublic => Field.IsPublic;
}
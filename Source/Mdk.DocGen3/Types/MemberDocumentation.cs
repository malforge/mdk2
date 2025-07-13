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
        ObsoleteMessage = obsoleteMessage;
        WhitelistKey = Whitelist.GetKey(memberReference);
        DocKey = Doc.GetDocKey(memberReference);
        FullName = memberReference.GetCSharpName();
        AssemblyName = memberReference.Module.Assembly.Name.Name ?? string.Empty;
        Namespace = memberReference is TypeReference typeReference ? typeReference.Namespace : memberReference.DeclaringType?.Namespace ?? string.Empty;
        Name = memberReference.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    }

    public abstract bool IsPublic { get; }

    MemberReference IMemberDocumentation.Member => _member;

    public DocMember? Documentation { get; }
    public bool IsObsolete => ObsoleteMessage is not null;
    public string? ObsoleteMessage { get; }
    public string WhitelistKey { get; }
    public string DocKey { get; }
    public string FullName { get; }
    public string AssemblyName { get; }
    public string Namespace { get; }
    public string Name { get; }
    public abstract string Title { get; }
    public abstract string ShortSignature();
}
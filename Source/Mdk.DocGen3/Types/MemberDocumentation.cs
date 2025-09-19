using System.Web;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public abstract class MemberDocumentation : IMemberDocumentation
{
    readonly MemberReference? _member;

    protected MemberDocumentation(string namespaceName, string assemblyName)
    {
        Parent = null;
        _member = null;
        Documentation = null;
        ObsoleteMessage = null;
        WhitelistKey = null!;
        DocKey = null!;
        FullName = namespaceName;
        AssemblyName = assemblyName;
        Namespace = namespaceName;
        Name = namespaceName;
    }

    protected MemberDocumentation(MemberDocumentation? parent, MemberReference memberReference, DocMember? docMember, string? obsoleteMessage = null)
    {
        Parent = parent;
        _member = memberReference ?? throw new ArgumentNullException(nameof(memberReference), "Member reference cannot be null.");
        Documentation = docMember;
        ObsoleteMessage = obsoleteMessage;
        WhitelistKey = Whitelist.GetKey(memberReference);
        DocKey = Doc.GetDocKey(memberReference);
        FullName = memberReference.GetCSharpName();
        AssemblyName = memberReference.GetAssemblyName().Name;
        Namespace = memberReference is TypeReference typeReference ? typeReference.Namespace : memberReference.DeclaringType?.Namespace ?? string.Empty;
        Name = memberReference.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    }

    MemberReference? IMemberDocumentation.Member => _member;
    public MemberDocumentation? Parent { get; protected set; }
    public DocMember? Documentation { get; }
    public bool IsObsolete => ObsoleteMessage is not null;
    public string? ObsoleteMessage { get; }
    public string WhitelistKey { get; }
    public string DocKey { get; }
    public string FullName { get; }
    public string AssemblyName { get; }
    public string Namespace { get; protected init; }
    public string Name { get; }
    public abstract string Title { get; }
    public abstract string ShortSignature();
    public abstract bool IsPublic();
    string? _slug;
    string? _fullyQualifiedName;

    protected virtual string GenerateSlug()
    {
        var type = _member?.DeclaringType ?? throw new InvalidOperationException("Member does not have a declaring type.");
        return HttpUtility.UrlEncode(type.Namespace) + "/" + 
               HttpUtility.UrlEncode(type.GetCSharpName(CSharpNameFlags.NestedParent | CSharpNameFlags.Name | CSharpNameFlags.Generics))
               + "?#" + HttpUtility.UrlEncode(ShortSignature()) + ".html";
    }
    
    public string Slug => _slug ??= GenerateSlug();
    
    public string FullyQualifiedName => _fullyQualifiedName ??= _member?.GetFullyQualifiedName() ?? throw new InvalidOperationException("Member does not have a fully qualified name.");

    public bool IsInheritedFor(TypeDocumentation typePage)
    {
        if (_member?.DeclaringType == null)
            return false;

        return typePage.Members(false).Contains(this);
    }

    public abstract bool IsExternal();
}
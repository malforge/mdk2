using System.Web;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class NamespaceDocumentation : MemberDocumentation
{
    readonly List<TypeDefinition> _members;

    NamespaceDocumentation(string namespaceName, string assemblyName, List<TypeDefinition> members) : base(namespaceName, assemblyName)
    {
        _members = members;
        Namespace = namespaceName;
        Title = namespaceName;
    }

    public override string Title { get; }
    public override string ShortSignature() => Namespace;
    public IEnumerable<TypeDefinition> Types() => _members;

    public override bool IsPublic() => true;

    public override bool IsExternal() => false;

    protected override string GenerateSlug() => HttpUtility.UrlEncode(Namespace);

    public class Builder(string namespaceName, string assemblyName) 
    {
        private readonly List<TypeDefinition> _members = new();
        private readonly string _namespaceName = namespaceName;
        readonly string _assemblyName = assemblyName;

        public Builder WithAdditionalType(TypeDefinition member)
        {
            _members.Add(member);
            return this;
        }

        public NamespaceDocumentation Build() => new(_namespaceName, _assemblyName, _members);
    }
}
using System.Web;
using Mdk.DocGen3.Support;
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
            if (member == null) throw new ArgumentNullException(nameof(member));
            // if (string.IsNullOrEmpty(member.Namespace))
            //     throw new ArgumentException("Type must have a namespace.", nameof(member));
            // Does this member already exist (check by fullname and slug)?
            var existingMember = _members.FirstOrDefault(m => m.GetFullyQualifiedName() == member.GetFullyQualifiedName());
            if (existingMember != null)
            {
                // If the existing member is different, throw an exception.
                var emfqn = existingMember.GetFullyQualifiedName();
                throw new InvalidOperationException($"Type with name '{member.Name}' already exists in namespace '{_namespaceName}'. Existing type: {emfqn}");
            }
            _members.Add(member);
            return this;
        }

        public NamespaceDocumentation Build() => new(_namespaceName, _assemblyName, _members);
    }
}
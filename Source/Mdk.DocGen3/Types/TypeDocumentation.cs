using System.Web;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class TypeDocumentation : MemberDocumentation
{
    readonly List<TypeDocumentation> _interfaces;
    readonly List<MemberDocumentation> _members;

    string? _shortSignature;

    private TypeDocumentation(TypeDefinition type, List<MemberDocumentation> members, List<TypeDocumentation> interfaces, string? obsoleteMessage, bool isExternalType) : base(null, type, null, obsoleteMessage)
    {
        Type = type;
        IsExternalType = isExternalType;
        Title = $"{type.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics | CSharpNameFlags.NestedParent)} {type.GetMemberTypeName()}{(type.IsObsolete() ? " (Obsolete)" : "")}";
        _members = members ?? throw new ArgumentNullException(nameof(members));
        _interfaces = interfaces;
    }

    public TypeDefinition Type { get; }
    public bool IsExternalType { get; }
    public override sealed string Title { get; }

    public TypeDocumentation? BaseType { get; protected set; }
    public IEnumerable<TypeDocumentation> Interfaces => _interfaces;
    public bool IsNested => Type.IsNested;

    public IEnumerable<MethodDocumentation> Constructors() => Members(false).OfType<MethodDocumentation>().Where(m => m.IsConstructor);
    public IEnumerable<FieldDocumentation> Fields(bool inherited = true) => Members(inherited).OfType<FieldDocumentation>();
    public IEnumerable<PropertyDocumentation> Properties(bool inherited = true) => Members(inherited).OfType<PropertyDocumentation>();
    public IEnumerable<MethodDocumentation> Methods(bool inherited = true) => Members(inherited).OfType<MethodDocumentation>().Where(m => !m.IsConstructor);
    public IEnumerable<EventDocumentation> Events(bool inherited = true) => Members(inherited).OfType<EventDocumentation>();
    public IEnumerable<TypeDocumentation> NestedTypes(bool inherited = true) => Members(inherited).OfType<TypeDocumentation>();

    public IEnumerable<MemberDocumentation> Members(bool inherited = true)
    {
        var myTypeName = Type.FullName;
        // If inherited is false, we need to filter out members that are inherited from base types or interfaces.
        foreach (var member in _members)
        {
            if (!inherited)
            {
                var m = ((IMemberDocumentation) member).Member;
                var memberDeclaringTypeName = m.DeclaringType?.FullName;
                if (!string.Equals(myTypeName, memberDeclaringTypeName, StringComparison.Ordinal))
                    continue;
            }
            yield return member;
        }
    }

    public override sealed string ShortSignature()
    {
        if (_shortSignature is not null)
            return _shortSignature;
        return _shortSignature = Type.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    }

    public override bool IsPublic() => Type.IsPublic || Type.IsNestedPublic;
    public override bool IsExternal() => Type.IsMsType();

    protected override string GenerateSlug()
    {
        return HttpUtility.UrlEncode(Type.Namespace) + "/" + HttpUtility.UrlEncode(Type.GetCSharpName(CSharpNameFlags.NestedParent | CSharpNameFlags.Name | CSharpNameFlags.Generics)) + ".html";
    }

    public class Builder
    {
        readonly NamespaceDocumentation.Builder _ns;
        readonly TypeDocumentation _doc;
        readonly List<Builder> _interfaceBuilders = new();
        readonly List<TypeDocumentation> _interfaces = new();
        readonly List<MemberDocumentation> _members = new();
        Builder? _baseTypeBuilder;
        string? _fullyQualifiedName;

        public Builder(NamespaceDocumentation.Builder ns, TypeDefinition type, string? obsoleteMessage = null) : this(ns, type, obsoleteMessage, false) { }

        private Builder(NamespaceDocumentation.Builder ns, TypeDefinition type, string? obsoleteMessage, bool isExternalType)
        {
            _ns = ns;
            _doc = new TypeDocumentation(type, _members, _interfaces, obsoleteMessage, isExternalType);
        }

        public TypeDocumentation Instance => _doc;
        
        public TypeDefinition Type => _doc.Type;
        public string FullyQualifiedName => _fullyQualifiedName ??= Type.GetFullyQualifiedName();
        // public string WhitelistKey => _doc.WhitelistKey;
        // public string DocKey => _doc.DocKey;

        public static Builder ForExternalType(TypeDefinition type, string? obsoleteMessage = null) => new(null!, type, obsoleteMessage, true);

        public Builder WithAdditionalMember(MemberDocumentation member)
        {
            if (member is null)
                throw new ArgumentNullException(nameof(member));
            var existingMember = _members.FirstOrDefault(m => m.FullyQualifiedName == member.FullyQualifiedName);
            if (existingMember != null)
            {
                // If the existing member is different, throw an exception.
                throw new InvalidOperationException($"Member with name '{member.ShortSignature()}' already exists in type '{_doc.Type.FullName}'.");
            }
            _members.Add(member);
            return this;
        }

        public Builder WithBaseType(Builder? baseTypeBuilder)
        {
            _baseTypeBuilder = baseTypeBuilder;
            return this;
        }

        public Builder WithInterface(Builder interfaceBuilder)
        {
            if (interfaceBuilder is null)
                throw new ArgumentNullException(nameof(interfaceBuilder));
            // Check if the interface is already added
            if (_interfaceBuilders.Any(b => b.FullyQualifiedName == interfaceBuilder.FullyQualifiedName))
                throw new InvalidOperationException($"Interface '{interfaceBuilder.Type.FullName}' is already added to type '{_doc.Type.FullName}'.");
            _interfaceBuilders.Add(interfaceBuilder);
            return this;
        }

        public TypeDocumentation Build()
        {
            _interfaces.Clear();
            _interfaces.AddRange(_interfaceBuilders.Select(b => b.Build()));
            _doc.Parent = _ns.Build();
            _doc.BaseType = _baseTypeBuilder?.Build();
            return _doc;
        }
    }

    // public IEnumerable<IMemberDocumentation> Members(bool inherited = false)
    // {
    //     foreach (var field in Fields)
    //         yield return field;
    //     foreach (var property in Properties)
    //         yield return property;
    //     foreach (var method in Methods)
    //         yield return method;
    //     foreach (var evt in Events)
    //         yield return evt;
    //     foreach (var nestedType in NestedTypes)
    //         yield return nestedType;
    //
    //     if (inherited) { }
    // }
}
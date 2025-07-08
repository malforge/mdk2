using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class TypeDocumentation(TypeDefinition type, DocMember? documentation): IMemberDocumentation
{
    string? _shortSignature;
    public TypeDefinition Type { get; } = type;
    IMemberDefinition IMemberDocumentation.Member => Type;
    public DocMember? Documentation { get; } = documentation;
    public string WhitelistKey { get; } = Whitelist.GetTypeKey(type);
    public string DocKey { get; } = Doc.GetDocKey(type);
    public string FullName { get; } = type.GetCSharpName();
    public string AssemblyName { get; } = type.Module.Assembly.Name.Name;
    public string Namespace { get; } = type.Namespace;
    public string Title { get; }  = $"{type.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics | CSharpNameFlags.NestedParent)} {type.GetMemberTypeName()}{(type.IsObsolete()? " (Obsolete)" : "")}";

    public string ShortSignature()
    {
        if (_shortSignature is not null)
            return _shortSignature;
        return _shortSignature = Type.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    }

    public List<FieldDocumentation> Fields { get; } = new();
    public List<PropertyDocumentation> Properties { get; } = new();
    public List<MethodDocumentation> Methods { get; } = new();
    public List<EventDocumentation> Events { get; } = new();
    public List<TypeDocumentation> NestedTypes { get; } = new();
    
    public IEnumerable<IMemberDocumentation> Members()
    {
        foreach (var field in Fields)
            yield return field;
        foreach (var property in Properties)
            yield return property;
        foreach (var method in Methods)
            yield return method;
        foreach (var evt in Events)
            yield return evt;
        foreach (var nestedType in NestedTypes)
            yield return nestedType;
    }
}
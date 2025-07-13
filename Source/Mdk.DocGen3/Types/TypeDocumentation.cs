using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class TypeDocumentation(TypeDefinition type, DocMember? documentation, string? obsoleteMessage = null) :
    MemberDocumentation(type, documentation, obsoleteMessage)
{
    string? _shortSignature;
    public TypeDefinition Type { get; } = type;
    public override bool IsPublic { get; } = type.IsPublic || type.IsNestedPublic;
    public override sealed string Title { get; } = $"{type.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics | CSharpNameFlags.NestedParent)} {type.GetMemberTypeName()}{(type.IsObsolete() ? " (Obsolete)" : "")}";

    public List<FieldDocumentation> Fields { get; } = new();
    public List<PropertyDocumentation> Properties { get; } = new();
    public List<MethodDocumentation> Methods { get; } = new();
    public List<EventDocumentation> Events { get; } = new();
    public List<TypeDocumentation> NestedTypes { get; } = new();

    public override sealed string ShortSignature()
    {
        if (_shortSignature is not null)
            return _shortSignature;
        return _shortSignature = Type.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    }

    public IEnumerable<IMemberDocumentation> Members(bool inherited = false)
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

        if (inherited) { }
    }
}
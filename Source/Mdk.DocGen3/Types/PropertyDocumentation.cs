using System.Text;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class PropertyDocumentation(PropertyDefinition property, DocMember? documentation)
    : IMemberDocumentation
{
    string? _shortSignature;
    public PropertyDefinition Property { get; } = property;
    IMemberDefinition IMemberDocumentation.Member => Property;
    public DocMember? Documentation { get; } = documentation;
    public string WhitelistKey { get; } = Whitelist.GetPropertyKey(property);
    public string DocKey { get; } = Doc.GetDocKey(property);
    public string FullName { get; } = property.GetCSharpName();
    public string AssemblyName { get; } = property.Module.Assembly.Name.Name;
    public string Namespace { get; } = property.DeclaringType.Namespace;
    public string Title { get; } = $"{property.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {property.PropertyType.GetMemberTypeName()}{(property.IsObsolete()? " (Obsolete)" : "")}";

    public string ShortSignature()
    {
        if (_shortSignature is not null)
            return _shortSignature;
        var builder = new StringBuilder();
        builder.Append(Property.Name);
        if (Property.Parameters.Count > 0)
        {
            builder.Append('[');
            for (var i = 0; i < Property.Parameters.Count; i++)
            {
                if (i > 0)
                    builder.Append(", ");
                var parameter = Property.Parameters[i];
                builder.Append(parameter.ParameterType.GetCSharpName(CSharpNameFlags.Name));
            }
            builder.Append(']');
        }
        return _shortSignature = builder.ToString();
    }

    public bool IsPublic => Property.GetMethod?.IsPublic == true || Property.SetMethod?.IsPublic == true;
}
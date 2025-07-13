using System.Text;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class PropertyDocumentation(PropertyDefinition property, DocMember? documentation, string? obsoleteMessage = null)
    : MemberDocumentation(property, documentation, obsoleteMessage)
{
    string? _shortSignature;
    public PropertyDefinition Property { get; } = property;
    public override sealed string Title { get; } = $"{property.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {property.PropertyType.GetMemberTypeName()}{(property.IsObsolete() ? " (Obsolete)" : "")}";
    public override sealed bool IsPublic => Property.GetMethod?.IsPublic == true || Property.SetMethod?.IsPublic == true;

    public override sealed string ShortSignature()
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
}
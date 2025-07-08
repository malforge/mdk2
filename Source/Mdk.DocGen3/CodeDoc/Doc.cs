using System.Text;
using System.Xml.Serialization;
using Mono.Cecil;

namespace Mdk.DocGen3.CodeDoc;

[XmlRoot("doc")]
public class Doc
{
    [XmlElement("assembly")]
    public DocAssembly? Assembly { get; set; }

    [XmlArray("members")]
    [XmlArrayItem("member")]
    public List<DocMember> Members { get; } = new();

    public static string GetDocKey(MemberReference memberReference)
    {
        return memberReference.Resolve() switch
        {
            TypeDefinition typeDef => GetDocKey(typeDef),
            FieldDefinition fieldDef => GetDocKey(fieldDef),
            PropertyDefinition propertyDef => GetDocKey(propertyDef),
            MethodDefinition methodDef => GetDocKey(methodDef),
            EventDefinition eventDef => GetDocKey(eventDef),
            _ => throw new ArgumentException($"Unsupported member type: {memberReference.GetType()}")
        };
    }
    
    
    public static string GetDocKey(TypeDefinition typeDef) => $"T:{GetTypeFullName(typeDef)}";

    static string GetTypeFullName(TypeDefinition typeDef)
    {
        StringBuilder sb = new();

        if (typeDef.IsNested)
            sb.Append(GetTypeFullName(typeDef.DeclaringType)).Append('+').Append(typeDef.Name);
        else
            sb.Append(typeDef.FullName);
        if (typeDef.IsGenericInstance)
        {
            sb.Append('{');
            sb.Append(string.Join(",", typeDef.GenericParameters.Select(p => GetTypeFullName(p.Resolve()))));
            sb.Append('}');
        }

        return sb.ToString();
    }

    public DocMember? GetDocumentation(string docKey) => Members.FirstOrDefault(m => m.Name == docKey);

    public static string GetDocKey(FieldDefinition fieldDef) => $"F:{GetTypeFullName(fieldDef.DeclaringType)}.{fieldDef.Name}";

    public static string GetDocKey(PropertyDefinition propertyDef)
    {
        return $"P:{GetTypeFullName(propertyDef.DeclaringType)}.{propertyDef.Name}";
    }

    public static string GetDocKey(MethodDefinition methodDef)
    {
        StringBuilder sb = new();
        sb.Append("M:");
        if (methodDef.IsConstructor)
            sb.Append(GetTypeFullName(methodDef.DeclaringType)).Append(".#ctor");
        else
            sb.Append(GetTypeFullName(methodDef.DeclaringType)).Append('.').Append(methodDef.Name);
        if (methodDef.IsGenericInstance)
        {
            sb.Append('{');
            sb.Append(string.Join(",", methodDef.GenericParameters.Select(p => GetTypeFullName(p.Resolve()))));
            sb.Append('}');
        }
        var parameters = string.Join(",", methodDef.Parameters.Select(p => p.ParameterType.FullName + (p.IsIn || p.IsOut ? "@" : "")));
        if (!string.IsNullOrEmpty(parameters))
            sb.Append('(').Append(parameters).Append(')');
        
        return sb.ToString();
    }

    public static string GetDocKey(EventDefinition eventDef)
    {
        StringBuilder sb = new();
        sb.Append(GetTypeFullName(eventDef.DeclaringType)).Append('.').Append(eventDef.Name);
        return sb.ToString();
    }
}
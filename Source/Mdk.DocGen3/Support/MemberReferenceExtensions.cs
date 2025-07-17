using System.Diagnostics;
using System.Text;
using Mono.Cecil;

namespace Mdk.DocGen3.Support;

public static class MemberReferenceExtensions
{
    /*
     *  public bool IsMicrosoftType()
//     {
//         var assembly = DeclaringType?.Module?.Assembly;
//         if (assembly is null)
//             return false;
//         var companyAttribute = assembly.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == "AssemblyCompanyAttribute");
//         if (companyAttribute is null)
//             return false;
//         var companyName = companyAttribute.ConstructorArguments.FirstOrDefault().Value as string;
//         if (string.IsNullOrEmpty(companyName))
//             return false;
//         return companyName.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
//     }
     */

    public static bool IsSuperTypeOf(this TypeReference potentialSuperType, TypeReference subType)
    {
       // Check if the type is the same or a base type of the typePage
        if (potentialSuperType == subType)
            return true;

        // Check if the type is a base type of the typePage
        var currentType = subType;
        while (currentType != null)
        {
            if (currentType == potentialSuperType)
                return true;
            currentType = currentType.Resolve()?.BaseType;
        }

        return false;
    }

    public static bool IsMsType(this MemberReference member)
    {
        return member switch
        {
            TypeReference typeReference => IsMsType(typeReference),
            MethodReference methodReference => IsMsType(methodReference.DeclaringType),
            FieldReference fieldReference => IsMsType(fieldReference.DeclaringType),
            PropertyReference propertyReference => IsMsType(propertyReference.DeclaringType),
            EventReference eventReference => IsMsType(eventReference.DeclaringType),
            _ => false
        };
    }
    
    static bool IsMsType(TypeReference type)
    {
        string n;
        if (type.Scope is AssemblyNameReference asmRef)
        {
            n = asmRef.Name;
        }
        else if (type.Scope is ModuleDefinition modDef)
        {
            n = modDef.Name;
        }
        else
        {
            Debugger.Break();
            n = null!;
        }

        if (n == "mscorlib"
            || n == "System"
            || n.StartsWith("System.", StringComparison.Ordinal)
            || n.StartsWith("Microsoft.", StringComparison.Ordinal))
        {
            return true;
        }

        var assemblyCompanyAttribute = type.Module.Assembly.CustomAttributes
            .FirstOrDefault(attr => attr.AttributeType.FullName == "System.Reflection.AssemblyCompanyAttribute");
        return assemblyCompanyAttribute?.ConstructorArguments.Count > 0 && assemblyCompanyAttribute.ConstructorArguments[0].Value is string company && company.Contains("Microsoft", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsObsolete(this MemberReference member)
    {
        if (member is not IMemberDefinition memberDefinition)
            return false;

        return memberDefinition.CustomAttributes.Any(attr => attr.AttributeType.FullName == "System.ObsoleteAttribute");
    }

    public static string? GetObsoleteMessage(this MemberReference member)
    {
        if (member is not IMemberDefinition memberDefinition)
            return null;

        var obsoleteAttribute = memberDefinition.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.FullName == "System.ObsoleteAttribute");
        if (obsoleteAttribute != null && obsoleteAttribute.ConstructorArguments.Count > 0)
            return obsoleteAttribute.ConstructorArguments[0].Value as string;
        return null;
    }

    public static string GetMemberTypeName(this MemberReference member) =>
        member switch
        {
            TypeDefinition { BaseType.FullName: "System.MulticastDelegate" } => "delegate",
            TypeDefinition { IsValueType: true } => "struct",
            TypeDefinition { IsClass: true } => "class",
            TypeDefinition { IsInterface: true } => "interface",
            TypeDefinition { IsEnum: true } => "enum",
            FieldDefinition { IsStatic: true, IsLiteral: true } => "constant",
            FieldDefinition => "field",
            MethodDefinition { IsConstructor: true } => "constructor",
            MethodDefinition => "method",
            PropertyDefinition { HasParameters: true } => "indexer",
            PropertyDefinition => "property",
            EventDefinition => "event",
            _ => "???"
        };

    public static string GetCSharpName(this MemberReference member, CSharpNameFlags flags = CSharpNameFlags.FullName) =>
        member switch
        {
            EventReference eventReference => EventToCSharpName(eventReference, flags),
            FieldReference fieldReference => FieldToCSharpName(fieldReference, flags),
            MethodReference methodReference => MethodToCSharpName(methodReference, flags),
            PropertyReference propertyReference => PropertyToCSharpName(propertyReference, flags),
            TypeReference typeReference => TypeToCSharpName(typeReference, flags),
            // PointerType pointerType => pointerType.ElementType.ToCSharpName(fullName) + "*",
            // ArrayType arrayType => $"{arrayType.ElementType.ToCSharpName(fullName)}[{new string(',', arrayType.Rank - 1)}]",
            // GenericInstanceType genericInstanceType => $"{genericInstanceType.ElementType.ToCSharpName(fullName)}<{string.Join(", ", genericInstanceType.GenericArguments.Select(g => g.ToCSharpName(fullName)))}>",
            // // ByReferenceType byReferenceType => ByReferenceTypeToCSharpName(byReferenceType, fullName),
            // GenericParameter genericParameter => genericParameter.Name,
            _ => throw new ArgumentOutOfRangeException(nameof(member), member, "Unsupported member type")
        };

    // switch (member)
    // {
    //     case EventReference eventReference:
    //         break;
    //     case FieldReference fieldReference:
    //         break;
    //     case MethodReference methodReference:
    //         break;
    //     case PropertyReference propertyReference:
    //         break;
    //     case TypeReference typeReference:
    //         
    //         break;
    //     default:
    //         throw new ArgumentOutOfRangeException(nameof(memberReference));
    // }
    static string EventToCSharpName(EventReference eventReference, CSharpNameFlags flags)
    {
        var sb = new StringBuilder();
        if (flags.HasFlag(CSharpNameFlags.NestedParent) && eventReference.DeclaringType != null)
        {
            sb.Append(eventReference.DeclaringType.GetCSharpName(flags));
            sb.Append('.');
        }

        if (flags.HasFlag(CSharpNameFlags.Name))
            sb.Append(eventReference.Name);

        return sb.ToString();
    }

    static string FieldToCSharpName(FieldReference fieldReference, CSharpNameFlags flags)
    {
        var sb = new StringBuilder();
        if (flags.HasFlag(CSharpNameFlags.NestedParent) && fieldReference.DeclaringType != null)
        {
            sb.Append(fieldReference.DeclaringType.GetCSharpName(flags));
            sb.Append('.');
        }

        if (flags.HasFlag(CSharpNameFlags.Name))
            sb.Append(fieldReference.Name);

        return sb.ToString();
    }

    static string MethodToCSharpName(MethodReference methodReference, CSharpNameFlags flags)
    {
        var sb = new StringBuilder();
        if (flags.HasFlag(CSharpNameFlags.NestedParent) && methodReference.DeclaringType != null)
        {
            sb.Append(methodReference.DeclaringType.GetCSharpName(flags));
            sb.Append('.');
        }
        if (flags.HasFlag(CSharpNameFlags.Name))
            sb.Append(methodReference.Name);

        if (methodReference.HasGenericParameters && flags.HasFlag(CSharpNameFlags.Generics))
            sb.Append('<').Append(string.Join(", ", methodReference.GenericParameters.Select(gp => gp.Name))).Append('>');

        if (flags.HasFlag(CSharpNameFlags.Parameters) && methodReference.Parameters.Count > 0)
        {
            sb.Append('(').Append(string.Join(", ",
                methodReference.Parameters.Select(p =>
                {
                    var type = p.ParameterType is ByReferenceType byRefType
                        ? byRefType.ElementType
                        : p.ParameterType;

                    return (p.IsIn ? "in " : p.IsOut ? "out " : "ref ") + GetCSharpName(type, flags);
                }))).Append(')');
        }

        return sb.ToString();
    }

    static string PropertyToCSharpName(PropertyReference property, CSharpNameFlags flags)
    {
        var sb = new StringBuilder();
        if (flags.HasFlag(CSharpNameFlags.NestedParent) && property.DeclaringType != null)
        {
            sb.Append(property.DeclaringType.GetCSharpName(flags));
            sb.Append('.');
        }

        if (flags.HasFlag(CSharpNameFlags.Name))
            sb.Append(property.Name);

        if (flags.HasFlag(CSharpNameFlags.Parameters) && property.Parameters.Count > 0)
        {
            var parameters = string.Join(", ", property.Parameters.Select(p => GetCSharpName(p.ParameterType, flags)));
            sb.Append($"[{parameters}]");
        }
        return sb.ToString();
    }

    static string TypeToCSharpName(TypeReference type, CSharpNameFlags flags)
    {
        if (type.IsGenericParameter)
            return type.Name;
        
        // If the type is one of the built-in types, return its name directly
        switch (type.MetadataType)
        {
            case MetadataType.Void:
                return "void";
            case MetadataType.Boolean:
                return "bool";
            case MetadataType.Char:
                return "char";
            case MetadataType.SByte:
                return "sbyte";
            case MetadataType.Byte:
                return "byte";
            case MetadataType.Int16:
                return "short";
            case MetadataType.UInt16:
                return "ushort";
            case MetadataType.Int32:
                return "int";
            case MetadataType.UInt32:
                return "uint";
            case MetadataType.Int64:
                return "long";
            case MetadataType.UInt64:
                return "ulong";
            case MetadataType.Single:
                return "float";
            case MetadataType.Double:
                return "double";
            case MetadataType.String:
                return "string";
            case MetadataType.Object:
                return "object";
        }

        if (flags.HasFlag(CSharpNameFlags.Generics))
        {
            if (type is GenericInstanceType genericInstanceType)
                return $"{genericInstanceType.ElementType.GetCSharpName(flags)}<{string.Join(", ", genericInstanceType.GenericArguments.Select(g => g.GetCSharpName(flags)))}>";
            if (type.HasGenericParameters)
                return $"{type.GetCSharpName(flags & ~CSharpNameFlags.Generics)}<{string.Join(", ", type.GenericParameters.Select(gp => gp.Name))}>";
        }

        if (type.IsArray)
            return $"{GetCSharpName(type.GetElementType(), flags)}[]";

        if (type.IsNested && flags.HasFlag(CSharpNameFlags.NestedParent))
        {
            var baseName = TypeToCSharpName(type.DeclaringType, flags) + ".";
            if (flags.HasFlag(CSharpNameFlags.Name) && !string.IsNullOrEmpty(type.Name))
                return $"{baseName}{type.Name}";
            return baseName;
        }

        string filter(string name) // Filter out generic parameters if not needed
        {
            var index = name.IndexOf('`');
            if (index < 0)
                return name;
            return name[..index];
        }

        var sb = new StringBuilder();
        if (flags.HasFlag(CSharpNameFlags.Namespace) && !string.IsNullOrEmpty(type.Namespace))
            sb.Append(type.Namespace).Append('.');

        if (flags.HasFlag(CSharpNameFlags.Name) && !string.IsNullOrEmpty(type.Name))
            sb.Append(type.HasGenericParameters? filter(type.Name) : type.Name);

        return sb.ToString();
    }
}
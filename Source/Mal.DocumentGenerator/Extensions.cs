using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public static class Extensions
{
    public static string GetCSharpName(this TypeReference type)
    {
        var name = type.Name;
        switch (type.FullName)
        {
            case "System.Void": return "void";
            case "System.Boolean": return "bool";
            case "System.Byte": return "byte";
            case "System.SByte": return "sbyte";
            case "System.Int16": return "short";
            case "System.UInt16": return "ushort";
            case "System.Int32": return "int";
            case "System.UInt32": return "uint";
            case "System.Int64": return "long";
            case "System.UInt64": return "ulong";
            case "System.Single": return "single";
            case "System.Double": return "double";
            case "System.String": return "string";
        }

        var sb = new StringBuilder();
        if (type.IsArray)
        {
            var array = (ArrayType)type;
            sb.Append(new DataType(array.ElementType)).Append("[]");
            return sb.ToString();
        }

        if (!string.IsNullOrEmpty(type.Namespace))
            sb.Append(type.Namespace).Append('.');
        if (type.HasGenericParameters)
        {
            var tickIndex = name.IndexOf('`');
            if (tickIndex < 0)
                sb.Append(name);
            else
            {
                sb.Append(type.Name.Substring(0, tickIndex));
                sb.Append('<');
                for (var i = 0; i < type.GenericParameters.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(type.GenericParameters[i].Name);
                }
                sb.Append('>');

                if (type.GenericParameters.Any(param => param.HasConstraints))
                {
                    sb.Append(" where ");
                    var first = true;
                    foreach (var param in type.GenericParameters)
                    {
                        if (!param.HasConstraints)
                            continue;

                        if (!first)
                            sb.Append(", ");
                        first = false;
                        sb.Append(param.Name);
                        sb.Append(" : ");
                        var firstConstraint = true;
                        foreach (var constraint in param.Constraints)
                        {
                            if (!firstConstraint)
                                sb.Append(", ");
                            firstConstraint = false;
                            sb.Append(new DataType(constraint.ConstraintType));
                        }
                    }
                }

                return sb.ToString();
            }
        }

        if (type.IsGenericInstance)
        {
            var generic = (GenericInstanceType)type;
            var tickIndex = type.Name.IndexOf('`');
            if (tickIndex < 0)
                sb.Append(type.Name);
            else
            {
                var baseName = type.Name.Substring(0, tickIndex);
                sb.Append(baseName);
                sb.Append('<');
                for (var i = 0; i < generic.GenericArguments.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    sb.Append(new DataType(generic.GenericArguments[i]));
                }
                sb.Append('>');
            }
            return sb.ToString();
        }

        return type.Name;
    }

    public static string GetCSharpName(this EventDefinition member)
    {
        var builder = new StringBuilder();
        builder.Append(GetCSharpName(member.DeclaringType))
            .Append('.')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string GetCSharpName(this PropertyDefinition member)
    {
        var builder = new StringBuilder();
        builder.Append(GetCSharpName(member.DeclaringType))
            .Append('.')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string GetCSharpName(this FieldDefinition member)
    {
        var builder = new StringBuilder();
        builder.Append(GetCSharpName(member.DeclaringType))
            .Append('.')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string GetCSharpName(this MethodDefinition member, bool asDisplayName = true)
    {
        var builder = new StringBuilder();
        var first = true;
        builder.Append(GetCSharpName(member.DeclaringType))
            .Append('.');
        if (member.HasGenericParameters)
        {
            builder.Append(member.Name)
                .Append('<');
            foreach (var param in member.GenericParameters)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                builder.Append(param.Name);
            }
            builder.Append('>');
        }
        else
            builder.Append(member.Name);

        builder.Append('(');
        first = true;
        foreach (var param in member.Parameters)
        {
            if (!first)
                builder.Append(',');

            if (param.IsIn)
                builder.Append("in ");
            else if (param.IsOut)
                builder.Append("out ");
            else if (param.ParameterType.IsByReference)
                builder.Append("ref ");

            first = false;
            builder.Append(GetCSharpName(param.ParameterType));

            if (asDisplayName) continue;

            if (param.HasDefault)
            {
                builder.Append(" = ");
                switch (param.Constant)
                {
                    case null:
                        builder.Append("null");
                        break;

                    case string str:
                        builder.Append('"').Append(str).Append('"');
                        break;

                    case char ch:
                        builder.Append('\'').Append(ch).Append('\'');
                        break;

                    case IFormattable formattable:
                        builder.Append(formattable.ToString(null, CultureInfo.InvariantCulture));
                        break;

                    default:
                        builder.Append(param.Constant);
                        break;
                }
            }
        }
        builder.Append(')');

        if (!member.HasGenericParameters) return builder.ToString();

        builder.Append(" where ");
        first = true;
        foreach (var param in member.GenericParameters)
        {
            if (!first)
                builder.Append(", ");
            first = false;
            builder.Append(param.Name);
            if (!param.HasConstraints) continue;
            builder.Append(" : ");
            first = true;
            foreach (var constraint in param.Constraints)
            {
                if (!first)
                    builder.Append(", ");
                first = false;
                builder.Append(GetCSharpName(constraint.ConstraintType));
            }
        }

        return builder.ToString();
    }

    public static string GetWhitelistName(this EventDefinition member)
    {
        var builder = new StringBuilder();
        builder.Append(GetWhitelistName(member.DeclaringType))
            .Append('.')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string GetWhitelistName(this PropertyDefinition member)
    {
        var builder = new StringBuilder();
        builder.Append(GetWhitelistName(member.DeclaringType))
            .Append('.')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string GetWhitelistName(this FieldDefinition member)
    {
        var builder = new StringBuilder();
        builder.Append(GetWhitelistName(member.DeclaringType))
            .Append('.')
            .Append(member.Name);
        return builder.ToString();
    }

    public static string GetWhitelistName(this MethodDefinition member)
    {
        var builder = new StringBuilder();
        var first = true;
        builder.Append(GetWhitelistName(member.DeclaringType))
            .Append('.');
        if (member.HasGenericParameters)
        {
            builder.Append(member.Name)
                .Append('<');
            foreach (var param in member.GenericParameters)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                builder.Append(param.Name);
            }
            builder.Append('>');
        }
        else
            builder.Append(member.Name);

        builder.Append('(');
        first = true;
        foreach (var param in member.Parameters)
        {
            if (!first)
                builder.Append(',');
            first = false;
            builder.Append(GetWhitelistName(param.ParameterType));
        }
        builder.Append(')');
        return builder.ToString();
    }

    public static string GetWhitelistName(this TypeReference type)
    {
        if (type.IsGenericParameter)
            return type.Name;

        var builder = new StringBuilder();
        if (type.IsNested)
        {
            builder.Append(GetWhitelistName(type.DeclaringType));
            builder.Append('.');
        }
        else
        {
            builder.Append(type.Namespace);
            if (!string.IsNullOrEmpty(type.Namespace))
                builder.Append('.');
        }

        if (type.HasGenericParameters && TryGetDistinctParameters(type, out var genericParameters))
        {
            var name = type.Name.Replace('/', '.');
            var endPt = name.IndexOf('`');
            builder.Append(name.Substring(0, endPt));
            builder.Append('<');
            var first = true;
            foreach (var arg in genericParameters)
            {
                if (!first)
                    builder.Append(',');
                first = false;
                builder.Append(GetWhitelistName(arg));
            }
            builder.Append('>');
        }
        else
            builder.Append(type.Name);

        return builder.ToString();
    }

    public static bool IsMicrosoftAssembly(this AssemblyDefinition assembly)
    {
        var microsoftPublicKeyTokens = new HashSet<string>
        {
            "b77a5c561934e089", // .NET Framework
            "7cec85d7bea7798e", // .NET Core/Standard
            "b03f5f7f11d50a3a", // .NET Framework alternate
            "31bf3856ad364e35" // ASP.NET
        };

        var publicKeyToken = GetPublicKeyToken(assembly);
        if (publicKeyToken != null && microsoftPublicKeyTokens.Contains(publicKeyToken))
            return true;

        // Additional heuristic: Check for attributes (e.g., AssemblyCompanyAttribute) for more clues
        foreach (var attribute in assembly.CustomAttributes)
        {
            if (attribute.AttributeType.FullName == "System.Reflection.AssemblyCompanyAttribute")
            {
                if (attribute.ConstructorArguments[0].Value is string companyName && companyName.Contains("Microsoft"))
                    return true;
            }
        }

        return false;
    }

    public static bool IsAccessible(this TypeDefinition typeDefinition)
    {
        if (typeDefinition.IsPublic)
            return true;

        if (typeDefinition.IsNestedPrivate)
            return false;

        if (!typeDefinition.IsNested)
            return false;

        if (!typeDefinition.DeclaringType.IsAccessible())
            return false;

        if (typeDefinition is { IsNestedAssembly: true, IsNestedFamily: false })
            return false;

        return !typeDefinition.DeclaringType.IsSealed;
    }

    public static bool IsAccessible(this MethodDefinition methodDefinition)
    {
        if (methodDefinition.IsPublic)
            return true;

        if (methodDefinition.IsPrivate)
            return false;

        if (methodDefinition is { IsAssembly: true, IsFamily: false })
            return false;

        return !methodDefinition.DeclaringType.IsSealed;
    }

    public static bool IsAccessible(this PropertyDefinition propertyDefinition) => (propertyDefinition.GetMethod?.IsAccessible() ?? false) || (propertyDefinition.SetMethod?.IsAccessible() ?? false);

    public static bool IsAccessible(this FieldDefinition fieldDefinition)
    {
        if (fieldDefinition.IsPublic)
            return true;

        if (fieldDefinition.IsPrivate)
            return false;

        if (fieldDefinition is { IsAssembly: true, IsFamily: false })
            return false;

        return !fieldDefinition.DeclaringType.IsSealed;
    }

    public static bool IsAccessible(this EventDefinition eventDefinition) => eventDefinition.AddMethod.IsAccessible() || eventDefinition.RemoveMethod.IsAccessible();

    public static bool IsDelegate(this TypeDefinition typeDefinition) => typeDefinition.BaseType is { FullName: "System.MulticastDelegate" or "System.Delegate" };

    public static bool IsExtensionMethod(this MethodDefinition method)
    {
        // Check if the method is static
        if (!method.IsStatic)
            return false;

        // Check if the method is defined in a class marked with the ExtensionAttribute
        if (method.DeclaringType.CustomAttributes.All(attr => attr.AttributeType.FullName != "System.Runtime.CompilerServices.ExtensionAttribute"))
            return false;

        if (method.CustomAttributes.Any(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute"))
            return true;

        return false;
    }

    public static bool IsAssignableFrom(this TypeReference? baseType, TypeReference? derivedType)
    {
        if (baseType == null || derivedType == null)
            return false;

        var baseTypeDef = baseType.Resolve();
        var derivedTypeDef = derivedType.Resolve();

        // Check if types are the same
        if (baseTypeDef == derivedTypeDef)
            return true;

        // Check base types
        while (derivedTypeDef != null)
        {
            if (derivedTypeDef.BaseType != null)
            {
                var resolvedBaseType = derivedTypeDef.BaseType.Resolve();
                if (resolvedBaseType == baseTypeDef)
                    return true;
                derivedTypeDef = resolvedBaseType;
            }
            else
                derivedTypeDef = null;
        }

        // Check interfaces
        derivedTypeDef = derivedType.Resolve(); // Resolve again for interface checking
        foreach (var interfaceImpl in derivedTypeDef.Interfaces)
        {
            if (interfaceImpl.InterfaceType.Resolve() == baseTypeDef)
                return true;
        }

        return false;
    }

    static bool TryGetDistinctParameters(TypeReference type, [MaybeNullWhen(false)] out List<GenericParameter> parameters)
    {
        if (!type.IsNested)
        {
            parameters = type.GenericParameters.ToList();
            return true;
        }

        parameters = null;
        foreach (var parameter in type.GenericParameters)
        {
            if (DefinesParameter(type.DeclaringType, parameter))
            {
                parameters ??= [];
                parameters.Add(parameter);
            }
        }

        return parameters != null;
    }

    static bool DefinesParameter(TypeReference? type, GenericParameter parameter)
    {
        if (type == null)
            return false;

        if (type.GenericParameters.Contains(parameter) || DefinesParameter(type.DeclaringType, parameter))
            return true;
        return false;
    }

    static string? GetPublicKeyToken(AssemblyDefinition assembly)
    {
        var bytes = assembly.Name.PublicKeyToken;
        if (bytes == null || bytes.Length == 0)
            return null;

        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }
}
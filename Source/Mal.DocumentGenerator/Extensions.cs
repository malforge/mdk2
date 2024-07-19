using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public static class Extensions
{
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

    static string? GetPublicKeyToken(AssemblyDefinition assembly)
    {
        var bytes = assembly.Name.PublicKeyToken;
        if (bytes == null || bytes.Length == 0)
            return null;

        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
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
}
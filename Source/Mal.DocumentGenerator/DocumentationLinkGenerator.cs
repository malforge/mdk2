using System.Web;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public static class DocumentationLinkGenerator
{
    private const string DefaultBaseUrl = "https://learn.microsoft.com/en-us/dotnet/api/";

    public static string GetDocumentationUrl(this TypeDefinition type, string? baseUrl = null)
    {
        baseUrl ??= DefaultBaseUrl;
        string typeName = GetFormattedTypeName(type);
        return $"{baseUrl}{typeName}";
    }

    public static string GetDocumentationUrl(this MethodDefinition method, string? baseUrl = null)
    {
        baseUrl ??= DefaultBaseUrl;
        string typeName = GetFormattedTypeName(method.DeclaringType);
        string methodName = method.IsConstructor ? (method.IsStatic ? ".cctor" : ".ctor") : method.Name.ToLowerInvariant();
        string parameters = GetParameterSignature(method);
        string fullMethodName = $"{typeName}.{methodName}{parameters}";

        if (method.HasGenericParameters)
        {
            fullMethodName += $"``{method.GenericParameters.Count}";
        }

        string encodedName = HttpUtility.UrlEncode(fullMethodName.Replace('/', '.'));
        return $"{baseUrl}{encodedName}";
    }

    public static string GetDocumentationUrl(this PropertyDefinition property, string? baseUrl = null)
    {
        baseUrl ??= DefaultBaseUrl;
        string typeName = GetFormattedTypeName(property.DeclaringType);
        string propertyName = property.Name.ToLowerInvariant();
        string fullPropertyName = $"{typeName}.{propertyName}";

        string encodedName = HttpUtility.UrlEncode(fullPropertyName.Replace('/', '.'));
        return $"{baseUrl}{encodedName}";
    }

    public static string GetDocumentationUrl(this FieldDefinition field, string? baseUrl = null)
    {
        baseUrl ??= DefaultBaseUrl;
        string typeName = GetFormattedTypeName(field.DeclaringType);
        string fieldName = field.Name.ToLowerInvariant();
        string fullFieldName = $"{typeName}.{fieldName}";

        string encodedName = HttpUtility.UrlEncode(fullFieldName.Replace('/', '.'));
        return $"{baseUrl}{encodedName}";
    }

    public static string GetDocumentationUrl(this EventDefinition @event, string? baseUrl = null)
    {
        baseUrl ??= DefaultBaseUrl;
        string typeName = GetFormattedTypeName(@event.DeclaringType);
        string eventName = @event.Name.ToLowerInvariant();
        string fullEventName = $"{typeName}.{eventName}";

        string encodedName = HttpUtility.UrlEncode(fullEventName.Replace('/', '.'));
        return $"{baseUrl}{encodedName}";
    }

    private static string GetParameterSignature(MethodDefinition method)
    {
        if (!method.HasParameters)
        {
            return string.Empty;
        }

        var sb = new System.Text.StringBuilder();
        sb.Append('(');
        for (int i = 0; i < method.Parameters.Count; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(method.Parameters[i].ParameterType.FullName.Replace('/', '.'));
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static string GetFormattedTypeName(TypeDefinition type)
    {
        string typeName = type.FullName.Replace('/', '.');
        if (type.HasGenericParameters)
        {
            typeName += $"-{type.GenericParameters.Count}";
        }
        if (type.DeclaringType != null)
        {
            typeName = $"{type.DeclaringType.FullName.Replace('/', '.')}.{typeName}";
        }
        return typeName.ToLowerInvariant();
    }
}
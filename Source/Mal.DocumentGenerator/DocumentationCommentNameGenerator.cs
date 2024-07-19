using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public static class DocumentationCommentNameGenerator
{
    public static string GetDocumentationCommentName(this TypeDefinition type)
    {
        var sb = new StringBuilder();
        sb.Append("T:").Append(type.GetFullName());
        return sb.ToString();
    }

    public static string GetDocumentationCommentName(this MethodDefinition method)
    {
        var sb = new StringBuilder();
        sb.Append("M:").Append(method.DeclaringType.GetFullName()).Append('.').Append(method.Name);

        if (method.HasParameters)
        {
            sb.Append('(');
            for (int i = 0; i < method.Parameters.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(method.Parameters[i].ParameterType.FullName);
            }
            sb.Append(')');
        }

        return sb.ToString();
    }

    public static string GetDocumentationCommentName(this PropertyDefinition property)
    {
        return $"P:{property.DeclaringType.GetFullName()}.{property.Name}";
    }

    public static string GetDocumentationCommentName(this FieldDefinition field)
    {
        return $"F:{field.DeclaringType.GetFullName()}.{field.Name}";
    }

    public static string GetDocumentationCommentName(this EventDefinition @event)
    {
        return $"E:{@event.DeclaringType.GetFullName()}.{@event.Name}";
    }

    public static string GetDocumentationCommentName(this ParameterDefinition parameter, MethodDefinition method)
    {
        return $"{method.GetDocumentationCommentName()}({parameter.ParameterType.FullName} {parameter.Name})";
    }

    private static string GetFullName(this TypeReference type)
    {
        var sb = new StringBuilder();
        if (type.DeclaringType != null)
        {
            sb.Append(type.DeclaringType.GetFullName()).Append('.');
        }
        else if (type.Namespace != null)
        {
            sb.Append(type.Namespace).Append('.');
        }

        sb.Append(type.Name.Replace('/', '.'));

        if (type is GenericInstanceType genericType)
        {
            sb.Append('{');
            for (int i = 0; i < genericType.GenericArguments.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(',');
                }
                sb.Append(genericType.GenericArguments[i].FullName);
            }
            sb.Append('}');
        }

        return sb.ToString();
    }
}
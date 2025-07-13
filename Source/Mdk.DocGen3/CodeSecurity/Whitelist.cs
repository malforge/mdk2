using Mono.Cecil;

namespace Mdk.DocGen3.CodeSecurity;

public class Whitelist
{
    readonly List<WhitelistRule> _whitelistRules;

    Whitelist(List<WhitelistRule> whitelistRules)
    {
        _whitelistRules = whitelistRules;
    }

    public static Whitelist Load(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentNullException(nameof(fileName));

        var lines = File.ReadAllLines(fileName);
        var rules = new List<WhitelistRule>();
        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;

            if (WhitelistRule.TryParse(trimmedLine, out var rule))
            {
                rules.Add(rule);
                continue;
            }

            throw new InvalidOperationException($"Invalid whitelist rule at line {index + 1}: {trimmedLine}");
        }

        if (rules.Count == 0)
            throw new InvalidOperationException($"No valid whitelist rules found in {fileName}");

        return new Whitelist(rules);
    }

    static string GetExpectedTypeName(TypeReference type)
    {
        // Built-in types like object, int, string etc.
        // should return those special names, not their full names.
        switch (type.FullName)
        {
            case "System.Object":
                return "object";
            case "System.Byte":
                return "byte";
            case "System.SByte":
                return "sbyte";
            case "System.Int16":
                return "short";
            case "System.UInt16":
                return "ushort";
            case "System.Int32":
                return "int";
            case "System.UInt32":
                return "uint";
            case "System.Int64":
                return "long";
            case "System.UInt64":
                return "ulong";
            case "System.Single":
                return "float";
            case "System.Double":
                return "double";
            case "System.Decimal":
                return "decimal";
            case "System.Boolean":
                return "bool";
            case "System.Char":
                return "char";
            case "System.String":
                return "string";
            case "System.Void":
                return "void";
        }

        // Whitelist compatible type name
        var typeName = type.FullName;
        if (type.IsNested)
        {
            var declaringType = type.DeclaringType;
            if (declaringType != null)
                typeName = $"{declaringType.FullName}+{typeName}";
        }

        // Remove generic parameters
        if (typeName.Contains('`'))
        {
            var index = typeName.IndexOf('`');
            typeName = typeName[..index];
        }

        // Add generic parameters: If not realized type, use the name, otherwise
        // generate the expected type name for each generic parameter
        if (type.HasGenericParameters)
        {
            // Is this a realized generic type, or a definition?
            string[]? genericArgs;
            if (type.IsGenericInstance)
            {
                genericArgs = type.GenericParameters.Select(g => GetExpectedTypeName(g.Resolve())).ToArray();
                typeName += $"<{string.Join(",", genericArgs)}>";
            }
            else
            {
                genericArgs = type.GenericParameters.Select(g => g.Name).ToArray();
                typeName += $"<{string.Join(",", genericArgs)}>";
            }
        }

        return typeName;
    }

    public static string GetKey(MemberReference member)
    {
        return member switch
        {
            TypeDefinition type => GetTypeKey(type),
            MethodDefinition method => GetMethodKey(method),
            FieldDefinition field => GetFieldKey(field),
            PropertyDefinition property => GetPropertyKey(property),
            EventDefinition eventDef => GetEventKey(eventDef),
            _ => throw new ArgumentException($"Unsupported member type: {member.GetType()}", nameof(member))
        };
    }
    
    public static string GetTypeKey(TypeDefinition type) => GetExpectedTypeName(type) + ", " + type.Module.Assembly.Name.Name;

    public static string GetMethodKey(MethodDefinition method)
    {
        var typeName = GetExpectedTypeName(method.DeclaringType);
        var methodName = method.Name;
        if (method.HasGenericParameters)
        {
            var genericArgs = method.GenericParameters.Select(g => g.Name).ToArray();
            methodName += $"<{string.Join(",", genericArgs)}>";
        }
        var parameters = string.Join(",", method.Parameters.Select(p => GetExpectedTypeName(p.ParameterType)));
        return $"{typeName}+{methodName}({parameters}), {method.Module.Assembly.Name.Name}";
    }

    public static string GetFieldKey(FieldDefinition field)
    {
        var typeName = GetExpectedTypeName(field.DeclaringType);
        var fieldName = field.Name;
        return $"{typeName}+{fieldName}, {field.Module.Assembly.Name.Name}";
    }

    public static string GetPropertyKey(PropertyDefinition property)
    {
        var typeName = GetExpectedTypeName(property.DeclaringType);
        var propertyName = property.Name;
        var parameters = string.Join(",", property.Parameters.Select(p => GetExpectedTypeName(p.ParameterType)));
        return $"{typeName}+{propertyName}({parameters}), {property.Module.Assembly.Name.Name}";
    }

    public static string GetEventKey(EventDefinition eventDef)
    {
        var typeName = GetExpectedTypeName(eventDef.DeclaringType);
        var eventName = eventDef.Name;
        return $"{typeName}+{eventName}, {eventDef.Module.Assembly.Name.Name}";
    }

    public bool IsAllowed(string typeKey) => _whitelistRules.Any(whitelistRule => whitelistRule.IsMatch(typeKey));
}
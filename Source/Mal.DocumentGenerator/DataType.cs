using System;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public readonly struct DataType
{
    public DataType(TypeReference type)
    {
        ArgumentNullException.ThrowIfNull(type);
        BackingType = type.Resolve();
        FullName = GetFullName(type);
        var lastPeriodIndex = FullName.LastIndexOf('.');
        Name = lastPeriodIndex < 0 ? FullName : FullName.Substring(lastPeriodIndex + 1);
        Namespace = lastPeriodIndex < 0 ? string.Empty : FullName.Substring(0, lastPeriodIndex);
    }

    public string Namespace { get; }

    public string Name { get; }

    public string FullName { get; }

    public TypeDefinition BackingType { get; }

    public override string ToString() => FullName;

    static string GetFullName(TypeReference type)
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
}
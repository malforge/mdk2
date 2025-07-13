using System.Text;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class MethodDocumentation(MethodDefinition method, DocMember? documentation)
    : IMemberDocumentation
{
    string? _shortSignature;
    public MethodDefinition Method { get; } = method;
    public bool IsConstructor => Method.IsConstructor;
    public bool IsPublic => Method.IsPublic;
    MemberReference IMemberDocumentation.Member => Method;
    public DocMember? Documentation { get; } = documentation;
    public string WhitelistKey { get; } = Whitelist.GetMethodKey(method);
    public string DocKey { get; } = Doc.GetDocKey(method);
    public string FullName { get; } = method.GetCSharpName();
    public string AssemblyName { get; } = method.Module.Assembly.Name.Name;
    public string Namespace { get; } = method.DeclaringType.Namespace;
    public string Title { get; } = $"{method.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {(method.IsObsolete() ? " (Obsolete)" : "")}";
    public string Name { get; } = method.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.Generics);
    public string ShortSignature()
    {
        if (_shortSignature is not null)
            return _shortSignature;
        var builder = new StringBuilder();
        if (Method.IsConstructor)
            builder.Append(Method.DeclaringType.GetCSharpName(CSharpNameFlags.Name));
        else
            builder.Append(Method.Name);

        builder.Append('(');
        var parameters = Method.Parameters;
        for (var i = 0; i < parameters.Count; i++)
        {
            if (i > 0)
                builder.Append(", ");
            var parameter = parameters[i];
            builder.Append(parameter.ParameterType.GetCSharpName(CSharpNameFlags.Name));
        }
        builder.Append(')');
        return _shortSignature = builder.ToString();
    }
}
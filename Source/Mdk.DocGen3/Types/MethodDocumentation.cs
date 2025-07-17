using System.Text;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.Support;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class MethodDocumentation(TypeDocumentation type, MethodDefinition method, DocMember? documentation, string? obsoleteMessage = null)
    : MemberDocumentation(type, method, documentation, obsoleteMessage)
{
    string? _shortSignature;
    public MethodDefinition Method { get; } = method;
    public bool IsConstructor => Method.IsConstructor;
    public override sealed string Title { get; } = $"{method.GetCSharpName(CSharpNameFlags.Name | CSharpNameFlags.NestedParent)} {(method.IsObsolete() ? " (Obsolete)" : "")}";
    public override bool IsPublic() => Method.IsPublic;
    public override bool IsExternal() => false;

    public override sealed string ShortSignature()
    {
        if (_shortSignature is not null)
            return _shortSignature;
        var builder = new StringBuilder();
        builder.Append(Method.IsConstructor ? Method.DeclaringType.GetCSharpName(CSharpNameFlags.Name) : Method.Name);

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
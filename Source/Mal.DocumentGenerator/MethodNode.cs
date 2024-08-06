using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class MethodNode(ITypeContext context, TypeNode parent, MethodDefinition method) : Node(context, method.GetDocumentationCommentName(), method.DeclaringType.Module.Assembly.Name.Name)
{
    string? _signature;
    public TypeNode ParentType { get; } = parent;
    public MethodDefinition Method { get; } = method;
    public string Name => Method.Name;
    public bool IsGeneric => Method.HasGenericParameters;
    public ImmutableArray<string> GenericParameters { get; } = method.HasGenericParameters ? [..method.GenericParameters.Select(p => p.Name)] : ImmutableArray<string>.Empty;
    public ImmutableArray<(DataType Type, string Name)> Parameters { get; } = [..method.Parameters.Select(p => (new DataType(p.ParameterType), p.Name))];
    public DataType ReturnType { get; } = new(method.ReturnType);
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];

    public override string Signature()
    {
        if (_signature == null)
        {
            var sb = new StringBuilder();
            if (Method.IsStatic) sb.Append("static ");
            // if (Method.IsVirtual) sb.Append("virtual ");
            // If the return type is not void
            if (Method.ReturnType.FullName != "System.Void")
            {
                sb.Append(Method.ReturnType.Name);
                sb.Append(' ');
            }
            sb.Append(Method.Name);
            if (IsGeneric)
            {
                sb.Append('<');
                for (var i = 0; i < GenericParameters.Length; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(GenericParameters[i]);
                }
                sb.Append('>');
            }
            sb.Append('(');
            for (var i = 0; i < Method.Parameters.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(Parameters[i].Type.Name);
            }
            sb.Append(')');
            _signature = sb.ToString();
        }
        return _signature;
    }

    public bool IsExtensionMethod() => Method.IsExtensionMethod();

    public IEnumerable<TypeNode> ExtensionTargets()
    {
        if (!IsExtensionMethod())
            yield break;

        var thisType = Method.Parameters[0].ParameterType;
        foreach (var type in Context.Types())
        {
            if (type.TypeDefinition.IsAssignableFrom(thisType))
                yield return type;
        }
    }
}
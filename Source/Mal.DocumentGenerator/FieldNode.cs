using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace Mal.DocumentGenerator;

public class FieldNode(ITypeContext context, TypeNode parent, FieldDefinition field) : Node(context, field.GetDocumentationCommentName(), field.DeclaringType.Module.Assembly.Name.Name)
{
    string? _signature;
    public TypeNode ParentType { get; } = parent;
    public FieldDefinition Field { get; } = field;
    public string Name => Field.Name;
    public DataType FieldType { get; } = new(field.FieldType);
    protected override Node GetParent() => ParentType;
    protected override IEnumerable<Node> EnumerateChildren() => [];
    public override string Signature()
    {
        if (_signature == null)
        {
            var sb = new StringBuilder();
            if (Field.IsStatic) sb.Append("static ");
            // IS it constant?
            if (Field.HasConstant)
                sb.Append("const ");
            sb.Append(FieldType.Name);
            sb.Append(' ');
            sb.Append(Field.Name);
            _signature = sb.ToString();
        }
        return _signature;
    }
}
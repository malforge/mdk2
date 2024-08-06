namespace Mal.DocumentGenerator.Dom;

public interface IDocTypeField : IDocTypeMember
{
    IDocType FieldType { get; }
    bool IsReadOnly { get; }
    bool IsConst { get; }
}
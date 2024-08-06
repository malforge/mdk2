namespace Mal.DocumentGenerator.Dom;

public interface IDocTypeMember : IDocTypeElement
{
    bool IsStatic { get; }
    string FullName { get; }
    string Name { get; }
    IDocType DeclaringType { get; }
}
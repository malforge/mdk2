namespace Mal.DocumentGenerator.Dom;

public interface IDocTypeExtensionMethod : IDocTypeMethod
{
    IDocType TargetType { get; }
}
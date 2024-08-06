namespace Mal.DocumentGenerator.Dom;

public interface IDocParameter
{
    string Name { get; }
    IDocType ParameterType { get; }
}
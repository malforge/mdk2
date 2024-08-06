namespace Mal.DocumentGenerator.Generator;

public class GeneratorContext(ITypeContext typeContext, string outputDirectory)
{
    public ITypeContext TypeContext { get; } = typeContext;
    public string OutputDirectory { get; } = outputDirectory;
}
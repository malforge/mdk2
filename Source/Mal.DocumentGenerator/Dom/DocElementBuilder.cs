namespace Mal.DocumentGenerator.Dom;

public abstract class DocElementBuilder(DocDomBuilder context)
{
    protected DocDomBuilder Context { get; } = context;
}
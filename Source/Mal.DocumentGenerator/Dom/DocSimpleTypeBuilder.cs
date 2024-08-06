namespace Mal.DocumentGenerator.Dom;

public abstract class DocSimpleTypeBuilder(DocDomBuilder context) : DocTypeBuilder(context)
{
    protected abstract class SimpleType(string fullName, string xmlDocId, string whitelistId) : DocType(fullName, xmlDocId, whitelistId), IDocSimpleType;
}
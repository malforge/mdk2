using System;
using System.Collections.Generic;

namespace Mal.DocumentGenerator.Dom;

public class DocEnumBuilder(DocDomBuilder context) : DocSimpleTypeBuilder(context)
{
    public override string Id => throw new NotImplementedException();
    public override IDocType Build() => throw new NotImplementedException();
    protected override void OnVisit() => throw new NotImplementedException();

    protected class Enum(string fullName, string xmlDocId, string whitelistId) : SimpleType(fullName, xmlDocId, whitelistId), IDocEnum
    {
        public override DocTypeKind Kind => DocTypeKind.Enum;
        public override IEnumerable<IDocTypeElement> Everything() => throw new NotImplementedException();
    }
}
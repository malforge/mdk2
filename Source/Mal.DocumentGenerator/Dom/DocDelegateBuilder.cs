using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Mal.DocumentGenerator.Dom;

public class DocDelegateBuilder(DocDomBuilder context) : DocSimpleTypeBuilder(context)
{
    public override string Id => throw new NotImplementedException();
    public override IDocType Build() => throw new NotImplementedException();
    protected override void OnVisit() => throw new NotImplementedException();

    protected class Delegate(string fullName, string xmlDocId, string whitelistId) : SimpleType(fullName, xmlDocId, whitelistId), IDocDelegate
    {
        public override DocTypeKind Kind => DocTypeKind.Delegate;

        public override IEnumerable<IDocTypeElement> Everything()
        {
            yield break;
        }

        public ImmutableArray<IDocGenericParameter> TypeParameters { get; set; }
    }
}
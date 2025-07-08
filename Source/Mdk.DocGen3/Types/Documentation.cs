using System.Collections.Immutable;
using Mdk.DocGen3.CodeDoc;
using Mono.Cecil;

namespace Mdk.DocGen3.Types;

public class Documentation(IEnumerable<TypeDocumentation> types)
{
    public ImmutableList<TypeDocumentation> Types { get; } = types.ToImmutableList();

    public IMemberDocumentation? GetMember(MemberReference memberReference)
    {
        return Types.FirstOrDefault(m => m.DocKey == Doc.GetDocKey(memberReference));
    }
}

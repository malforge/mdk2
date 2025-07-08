using System.Collections.Immutable;
using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class MethodPage(IEnumerable<MethodDocumentation> methodDocumentation)
    : Page
{
    public ImmutableList<MethodDocumentation> MethodDocumentation { get; } = methodDocumentation.ToImmutableList();
    protected override IMemberDocumentation GetMemberDocumentation() => MethodDocumentation[0];
}
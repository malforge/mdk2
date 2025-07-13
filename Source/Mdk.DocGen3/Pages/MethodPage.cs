using System.Collections.Immutable;
using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class MethodPage(IEnumerable<MethodDocumentation> methodDocumentation)
    : DocumentationPage
{
    public ImmutableList<MethodDocumentation> MethodDocumentation { get; } = methodDocumentation.ToImmutableList();
    public override IMemberDocumentation GetMemberDocumentation() => MethodDocumentation[0];
}
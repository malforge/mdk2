using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class FieldPage(FieldDocumentation fieldDocumentation) : Page
{
    public FieldDocumentation FieldDocumentation { get; } = fieldDocumentation;
    protected override IMemberDocumentation GetMemberDocumentation() => FieldDocumentation;
}
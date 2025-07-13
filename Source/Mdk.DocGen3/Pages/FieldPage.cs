using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class FieldPage(FieldDocumentation fieldDocumentation) : DocumentationPage
{
    public FieldDocumentation FieldDocumentation { get; } = fieldDocumentation;
    public override IMemberDocumentation GetMemberDocumentation() => FieldDocumentation;
}
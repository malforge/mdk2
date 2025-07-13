using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class PropertyPage(PropertyDocumentation propertyDocumentation) : DocumentationPage
{
    public PropertyDocumentation PropertyDocumentation { get; } = propertyDocumentation;

    public override IMemberDocumentation GetMemberDocumentation() =>
        PropertyDocumentation;
}
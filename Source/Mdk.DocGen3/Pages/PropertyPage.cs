using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class PropertyPage(PropertyDocumentation propertyDocumentation) : Page
{
    public PropertyDocumentation PropertyDocumentation { get; } = propertyDocumentation;

    protected override IMemberDocumentation GetMemberDocumentation() =>
        PropertyDocumentation;
}
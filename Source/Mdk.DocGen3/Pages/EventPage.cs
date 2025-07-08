using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class EventPage(EventDocumentation eventDocumentation) : Page
{
    public EventDocumentation EventDocumentation { get; } = eventDocumentation;
    protected override IMemberDocumentation GetMemberDocumentation() => EventDocumentation;
}
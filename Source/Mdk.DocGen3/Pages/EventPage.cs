using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class EventPage(EventDocumentation eventDocumentation) : DocumentationPage
{
    public EventDocumentation EventDocumentation { get; } = eventDocumentation;
    public override IMemberDocumentation GetMemberDocumentation() => EventDocumentation;
}
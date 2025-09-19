using Mdk.DocGen3.Pages.Base;
using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

// public abstract class MemberPageLayout : PageGeneratorLayout//<MemberPageGenerator>
// {
//     public string? Title { get; set; }
//     public string? CssSlug { get; set; }
//     public string? JsSlug { get; set; }
//     public IEnumerable<Breadcrumb>? Breadcrumbs { get; set; }
//     public string? Summary { get; set; }
//     public string? Remarks { get; set; }
//     public string? CommunityRemarks { get; set; }
//     public DateTimeOffset? Date { get; } = DateTimeOffset.UtcNow;
// }

public class MemberPageModelBase
{
    public string? Title { get; set; }
    public string? CssSlug { get; set; }
    public string? JsSlug { get; set; }
    public IEnumerable<Breadcrumb>? Breadcrumbs { get; set; }
    public string? Summary { get; set; }
    public string? Remarks { get; set; }
    public string? CommunityRemarks { get; set; }
    public DateTimeOffset? Date { get; set; } = DateTimeOffset.UtcNow;
}
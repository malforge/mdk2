namespace Mdk.DocGen3.Web;

public abstract class MemberPageModel: PageModel
{
    public string? HomeSlug { get; set; }
    public string? IndexSlug { get; set; }
    public string? IndexName { get; set; }
    public IEnumerable<Breadcrumb>? Breadcrumbs { get; set; }
}
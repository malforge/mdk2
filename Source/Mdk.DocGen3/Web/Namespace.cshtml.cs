using Mdk.DocGen3.Pages;

namespace Mdk.DocGen3.Web;

public class Namespace : MemberPageModel
{
    public string? Summary { get; set; }
    public string? Remarks { get; set; }
    public string? CommunityRemarks { get; set; }
    public IEnumerable<TypeItemModel>? Types { get; set; }

    public void Generate(Context context, string ns, IEnumerable<DocumentationPage> pages)
    {
        Title = ns;
        Slug = ns.Replace('.', '/') + "/index.html";
        IndexSlug = "index.html";
        IndexName = Title;
        HomeSlug = context.ToRelative(Slug, "/index.html");
        CssSlug = context.ToRelative(Slug, "/css/style.css");
        Breadcrumbs =
        [
            new Breadcrumb(HomeSlug, "Home")
        ];

        Summary = $"Documentation for the {ns} namespace.";
        CommunityRemarks = context.GetCommunityRemarksHtml($"N:{ns}");
        Types = pages.OrderBy(p => p.Title).Select(p => new TypeItemModel
        {
            Name = p.Name,
            Slug = p.Name + ".html",
            Summary = p.GetMemberDocumentation().Documentation?.RenderSummary() ?? ""
        }).ToList();

        var result = context.Engine.CompileRenderAsync("Namespace.cshtml", this).GetAwaiter().GetResult();
        var nsFolder = ns.Replace('.', Path.DirectorySeparatorChar);
        context.WriteHtml(Path.Combine(nsFolder, "index.html"), result);

        foreach (var page in pages)
        {
            var memberPage = new MemberPage();
            memberPage.Generate(context, this, page);
        }
    }
}
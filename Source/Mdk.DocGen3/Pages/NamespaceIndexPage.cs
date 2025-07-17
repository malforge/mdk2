using Mdk.DocGen3.Types;
using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class NamespaceIndexPage : MemberPageGenerator
{
    protected override string OnRender() =>
        $"""
         <div class="breadcrumbs">{RenderBreadcrumbs()}</div>
         <h1>{Esc(Title)} Namespace</h1>
         {RenderIf("summary", Summary)}
         {RenderIf("remarks", Remarks)}
         {RenderIf("community-remarks", CommunityRemarks)}
         <table>
             {RenderTypes()}
         </table>

         """;

    string RenderBreadcrumbs() => string.Join(" / ", Breadcrumbs?.Select(b => $"<a href=\"{b.Slug}\">{Esc(b.Name)}</a>") ?? []);

    string RenderIf(string cssClass, string? content) =>
        !string.IsNullOrEmpty(content)
            ? $"<p class=\"{cssClass}\">{content}</p>"
            : string.Empty;

    string RenderTypes() =>
        Types is not null && Types.Any()
            ? string.Join("\n",
                Types.Select(type =>
                    $"""
                     <tr>
                         <td><a href="{type.Slug}">{Esc(type.Name)}</a></td>
                         <td>{type.Summary}</td>
                     </tr>
                     """))
            : "<tr><td colspan=\"2\">No types found in this namespace.</td></tr>";
    
    public IEnumerable<TypeItemModel>? Types { get; set; }

    public static void Generate(Context context, NamespaceLayout layout, string ns, IReadOnlyList<MemberDocumentation> pages)
    {
        var slug = context.GetAddressOf(ns);
        var pge = new NamespaceIndexPage
        {
            Layout = layout,
            Title = ns,
            Breadcrumbs =
            [
                new Breadcrumb(context.ToRelative(slug, "/index.html"), "Home")
            ],
            CssSlug = context.ToRelative(slug, "/css/style.css"),
            JsSlug = context.ToRelative(slug, "/js/script.js"),
            Summary = $"Documentation for the {layout} namespace.",
            CommunityRemarks = context.GetCommunityRemarksHtml($"N:{layout}"),
            Types = pages.OrderBy(p => p.Title).Select(p => new TypeItemModel
            {
                Name = p.Name,
                Slug = p.Name + ".html",
                Summary = p.Documentation?.RenderSummary() ?? ""
            }).ToList()
        };

        context.WriteHtml(slug, pge.Render());

        foreach (var page in pages)
            MemberPage.Generate(context, layout, page);
    }
}
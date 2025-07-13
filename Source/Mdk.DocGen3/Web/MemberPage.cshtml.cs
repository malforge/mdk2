using Mdk.DocGen3.Pages;

namespace Mdk.DocGen3.Web;

public class MemberPage : Namespace
{
    public string? CommunityRemarks { get; set; }
    public string? Remarks { get; set; }
    public string? Namespace { get; set; }
    public string? Assembly { get; set; }
    public string? Date { get; set; }
    public string? Returns { get; set; }
    bool _isSpaceText = false;

    public IEnumerable<MemberTable>? MemberTables { get; set; }

    public void Generate(Context context, Namespace ns, DocumentationPage page)
    {
        _isSpaceText = page.GetMemberDocumentation().FullName == "Sandbox.Game.Localization.MySpaceTexts" || page.GetMemberDocumentation().FullName == "Sandbox.Game.Localization.MyCoreTexts";


        Title = page.Title;
        Namespace = page.Namespace;
        Assembly = page.GetMemberDocumentation().AssemblyName;
        Date = DateTimeOffset.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        var nsPath = Path.GetDirectoryName(ns.Slug);
        Slug = $"{nsPath}/{context.ToSafeFileName(page.Name)}.html";
        IndexSlug = context.ToRelative(Slug, "/index.html");
        IndexName = ns.Title ?? "Parent";
        CssSlug = context.ToRelative(Slug, "/css/style.css");
        var nsIndexSlug = context.ToRelative(Slug, $"{nsPath}/index.html");
        Breadcrumbs =
        [
            new Breadcrumb(IndexSlug, "Home"),
            new Breadcrumb(nsIndexSlug, ns.Title ?? "Parent")
        ];
        Summary = page.GetMemberDocumentation().Documentation?.RenderSummary() ?? "";
        // If this is the space texts, we want to add a blob to the summary explaining that we will
        // not list all the members of this type because there's too many of them.
        if (_isSpaceText)
        {
            Summary += """
                       <p class="warning">
                         This is a generated localization file. It contains a lot of members, 
                         so we will not list them all here. Please refer to the source code 
                         or your IDE intellisense for more details.
                       </p>
                       """;
        }

        Remarks = page.GetMemberDocumentation().Documentation?.RenderRemarks() ?? "";
        CommunityRemarks = context.GetCommunityRemarksHtml(page.GetMemberDocumentation().DocKey);
        Types = ns.Types;

        switch (page)
        {
            case TypePage typePage:
                Generate(context, typePage);
                break;
            case MethodPage methodPage:
                Generate(context, methodPage);
                break;
            case PropertyPage propertyPage:
                Generate(context, propertyPage);
                break;
            case EventPage eventPage:
                Generate(context, eventPage);
                break;
            case FieldPage fieldPage:
                Generate(context, fieldPage);
                break;
        }

        var result = context.Engine.CompileRenderAsync("MemberPage.cshtml", this).GetAwaiter().GetResult();
        context.WriteHtml(Slug, result);
    }

    void Generate(Context context, FieldPage fieldPage) =>
        Returns = fieldPage.GetMemberDocumentation()
            .Documentation?.RenderReturns();

    void Generate(Context context, EventPage eventPage) { }

    void Generate(Context context, PropertyPage propertyPage) =>
        Returns = propertyPage.GetMemberDocumentation()
            .Documentation?.RenderReturns();

    void Generate(Context context, MethodPage methodPage) =>
        Returns = methodPage.GetMemberDocumentation()
            .Documentation?.RenderReturns();

    void Generate(Context context, TypePage typePage)
    {
        if (_isSpaceText)
        {
            MemberTables = [];
            return;
        }

        MemberTables = ((List<MemberTable>)
        [
            new MemberTable
            {
                Title = "Constructors",
                Members = typePage.Constructors().Select(c => new MemberTableRow
                {
                    Name = c.ShortSignature(),
                    Slug = c.Name + ".html",
                    Summary = c.Documentation?.RenderSummary()
                })
            },
            new MemberTable
            {
                Title = "Events",
                Members = typePage.Events().Select(e => new MemberTableRow
                {
                    Name = e.ShortSignature(),
                    Slug = e.Name + ".html",
                    Summary = e.Documentation?.RenderSummary()
                })
            },
            new MemberTable
            {
                Title = "Fields",
                Members = typePage.Fields().Select(f => new MemberTableRow
                {
                    Name = f.ShortSignature(),
                    Slug = f.Name + ".html",
                    Summary = f.Documentation?.RenderSummary()
                })
            },
            new MemberTable
            {
                Title = "Properties",
                Members = typePage.Properties().Select(p => new MemberTableRow
                {
                    Name = p.ShortSignature(),
                    Slug = p.Name + ".html",
                    Summary = p.Documentation?.RenderSummary()
                })
            },
            new MemberTable
            {
                Title = "Methods",
                Members = typePage.Methods().Select(m => new MemberTableRow
                {
                    Name = m.ShortSignature(),
                    Slug = m.Name + ".html",
                    Summary = m.Documentation?.RenderSummary()
                })
            }
        ]).Where(t => !t.IsEmpty).ToList();
    }
}

public class MemberTable
{
    public string? Title { get; set; }
    public IEnumerable<MemberTableRow>? Members { get; set; }
    public bool IsEmpty => Members == null || !Members.Any();
}

public class MemberTableRow
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Summary { get; set; }
}
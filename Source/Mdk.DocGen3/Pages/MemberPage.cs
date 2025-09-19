using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using Mdk.DocGen3.Pages.Base;
using Mdk.DocGen3.Types;
using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class MemberPage : PageGenerator<MemberPageModel>
{
    string RenderBreadcrumbs() => Join(" ( ", Model.Breadcrumbs?.Select(b => $"<a href=\"{b.Slug}\">{Esc(b.Name)}</a>") ?? []);

    string RenderInterfaces() =>
        Model.Interfaces is not null && Model.Interfaces.Any()
            ? $"<span>Interfaces:</span> <span>{Join(", ", Model.Interfaces.Select(iface => $"<a href=\"{iface.Slug}\">{Esc(iface.Name)}</a>"))}</span>"
            : string.Empty;

    string RenderIf(string cssClass, string? content) =>
        !string.IsNullOrEmpty(content)
            ? $"<p class=\"{cssClass}\">{content}</p>"
            : string.Empty;

    string RenderTables() =>
        Model.MemberTables is not null && Model.MemberTables.Any()
            ? Join("\n", Model.MemberTables.Select(RenderTable))
            : "<p>No member tables available.</p>";

    string RenderTable(MemberTable arg) =>
        $"""
         <div class="{Css("member-list", arg.CustomCssClasses)}">
             <h2>{Esc(arg.Title)}</h2>
             <table>
                 <tbody>
                     {RenderTableRows(arg.Members?.ToList())}
                 </tbody>
             </table>
         </div>
         """;

    string RenderTableRows(IReadOnlyList<MemberTableRow>? members) =>
        members is not null && members.Any()
            ? Join("\n", members.Select(RenderMemberRow))
            : "<tr><td colspan=\"2\">No members found.</td></tr>";

    string RenderMemberRow(MemberTableRow row) =>
        $"""
         <tr>
             <td class="{Css("member-name", row.CustomCssClasses)}">
                 <a href="{row.Slug}">{Esc(row.Name)}</a>
             </td>
             <td class="member-description">{row.Summary}</td>
         </tr>
         """;

    protected override string OnRender() =>
        $"""
         <div class="breadcrumbs">{RenderBreadcrumbs()}</div>
         <h1>{Esc(Model.Title)}</h1>
         <h2>Definition</h2>
         <div class="definition">
            <span>Namespace:</span> <span class="value">@Model.Namespace</span><br/>
            <span>Assembly:</span> <span class="value">@Model.Assembly</span><br/>
            {RenderInterfaces()}
         </div>
         <p>
            <button id="toggleNonPublic" class="toggle-btn">
                Show non-public
            </button>
         </p>
         {RenderIf("summary", Model.Summary)}
         {RenderIf("remarks", Model.Remarks)}
         {RenderIf("community-remarks", Model.CommunityRemarks)}
         {RenderTables()}
         <footer>
            <div>Updated {Model.Date:u}</div>
         </footer>

         """;

    public static void Generate(Context context, NamespaceLayout layout, MemberDocumentation page)
    {
        var model = new MemberPageModel
        {
            Date = DateTime.UtcNow,
            Title = page.Title,
            CssSlug = context.ToRelative(page.Slug, "/css/style.css"),
            JsSlug = context.ToRelative(page.Slug, "/js/script.js"),
            Breadcrumbs =
            [
                new Breadcrumb(context.ToRelative(page.Slug, "/index.html"), "Home"),
                new Breadcrumb(context.ToRelative(page.Slug, page.Parent!.Slug), page.Parent?.Title ?? "Parent")
            ],
            Summary = page.Documentation?.RenderSummary() ?? "",
            Remarks = page.Documentation?.RenderRemarks() ?? "",
            CommunityRemarks = context.GetCommunityRemarksHtml(page.DocKey),
            Namespace = page.Namespace,
            Assembly = page.AssemblyName
        };
        var pge = new MemberPage
        {
            Layout = layout,
            Model = model
        };

        // switch (page)
        // {
        //     case TypeDocumentation typePage:
        //         Generate(context, pge, typePage);
        //         break;
        //     case MethodDocumentation methodPage:
        //         Generate(context, pge, methodPage);
        //         break;
        //     case PropertyDocumentation propertyPage:
        //         Generate(context, pge, propertyPage);
        //         break;
        //     case EventDocumentation eventPage:
        //         Generate(context, pge, eventPage);
        //         break;
        //     case FieldDocumentation fieldPage:
        //         Generate(context, pge, fieldPage);
        //         break;
        // }

        var result = pge.Render();
        context.WriteHtml(page.Slug, result);
    }

    // static void Generate(Context context, MemberPage pge, FieldDocumentation fieldPage) => pge.Returns = fieldPage.Documentation?.RenderReturns();
    //
    // static void Generate(Context context, MemberPage pge, EventDocumentation eventPage) { }
    //
    // static void Generate(Context context, MemberPage pge, PropertyDocumentation propertyPage) => pge.Returns = propertyPage.Documentation?.RenderReturns();
    //
    // static void Generate(Context context, MemberPage pge, MethodDocumentation methodPage) => pge.Returns = methodPage.Documentation?.RenderReturns();
    //
    // static void Generate(Context context, MemberPage pge, TypeDocumentation typePage)
    // {
    //     var isSpaceText = typePage.FullName is "Sandbox.Game.Localization.MySpaceTexts" or "Sandbox.Game.Localization.MyCoreTexts";
    //
    //     // If this is the space texts, we want to add a blob to the summary explaining that we will
    //     // not list all the members of this type because there's too many of them.
    //     if (isSpaceText)
    //     {
    //         pge.Summary += """
    //                        <p class="warning">
    //                          This is a generated localization file. It contains a lot of members, 
    //                          so we will not list them all here. Please refer to the source code 
    //                          or your IDE intellisense for more details.
    //                        </p>
    //                        """;
    //         pge.MemberTables = [];
    //         return;
    //     }
    //
    //     pge.Interfaces = typePage.Interfaces
    //         .Where(i => context.Whitelist.IsAllowed(i.WhitelistKey))
    //         .Select(i => new Breadcrumb(
    //             context.ToRelative(typePage.Slug, $"{i.Name}.html"),
    //             i.ShortSignature()
    //         )).ToList();
    //
    //     List<MemberTable> memberTables = [];
    //
    //     var constructors = typePage.Constructors().Where(c => context.Whitelist.IsAllowed(c.WhitelistKey)).ToList();
    //
    //     var publicConstructors = constructors.Where(c => c.IsPublic()).ToList();
    //     if (publicConstructors.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Constructors",
    //             Members = publicConstructors.Select(c => new MemberTableRow
    //             {
    //                 Name = c.ShortSignature(),
    //                 Slug = c.Name + ".html",
    //                 Summary = c.Documentation?.RenderSummary(),
    //                 CustomCssClasses = ["direct"]
    //             }).ToList()
    //         });
    //     }
    //     var nonPublicConstructors = constructors.Where(c => !c.IsPublic()).ToList();
    //     if (nonPublicConstructors.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Non-Public Constructors",
    //             CustomCssClasses = ["non-public"],
    //             Members = nonPublicConstructors.Select(c => new MemberTableRow
    //             {
    //                 Name = c.ShortSignature(),
    //                 Slug = c.Name + ".html",
    //                 Summary = c.Documentation?.RenderSummary(),
    //                 CustomCssClasses = ["direct"]
    //             }).ToList()
    //         });
    //     }
    //
    //     var fields = typePage.Fields().Where(c => context.Whitelist.IsAllowed(c.WhitelistKey)).ToList();
    //     var publicFields = fields.Where(f => f.IsPublic()).ToList();
    //     if (publicFields.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Fields",
    //             Members = publicFields.Select(f => new MemberTableRow
    //             {
    //                 Name = f.ShortSignature(),
    //                 Slug = f.Name + ".html",
    //                 Summary = f.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!f.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //
    //     var nonPublicFields = fields.Where(f => !f.IsPublic()).ToList();
    //     if (nonPublicFields.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Non-Public Fields",
    //             CustomCssClasses = ["non-public"],
    //             Members = nonPublicFields.Select(f => new MemberTableRow
    //             {
    //                 Name = f.ShortSignature(),
    //                 Slug = f.Name + ".html",
    //                 Summary = f.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!f.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //
    //     var properties = typePage.Properties().Where(c => context.Whitelist.IsAllowed(c.WhitelistKey)).ToList();
    //     var publicProperties = properties.Where(p => p.IsPublic()).ToList();
    //     if (publicProperties.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Properties",
    //             Members = publicProperties.Select(p => new MemberTableRow
    //             {
    //                 Name = p.ShortSignature(),
    //                 Slug = p.Name + ".html",
    //                 Summary = p.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!p.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //     var nonPublicProperties = properties.Where(p => !p.IsPublic()).ToList();
    //     if (nonPublicProperties.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Non-Public Properties",
    //             CustomCssClasses = ["non-public"],
    //             Members = nonPublicProperties.Select(p => new MemberTableRow
    //             {
    //                 Name = p.ShortSignature(),
    //                 Slug = p.Name + ".html",
    //                 Summary = p.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!p.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //
    //     var methods = typePage.Methods().Where(c => context.Whitelist.IsAllowed(c.WhitelistKey)).ToList();
    //     var publicMethods = methods.Where(m => m.IsPublic()).ToList();
    //     if (publicMethods.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Methods",
    //             Members = publicMethods.Select(m => new MemberTableRow
    //             {
    //                 Name = m.ShortSignature(),
    //                 Slug = m.Name + ".html",
    //                 Summary = m.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!m.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //     var nonPublicMethods = methods.Where(m => !m.IsPublic()).ToList();
    //     if (nonPublicMethods.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Non-Public Methods",
    //             CustomCssClasses = ["non-public"],
    //             Members = nonPublicMethods.Select(m => new MemberTableRow
    //             {
    //                 Name = m.ShortSignature(),
    //                 Slug = m.Name + ".html",
    //                 Summary = m.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!m.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //
    //     var events = typePage.Events().Where(c => context.Whitelist.IsAllowed(c.WhitelistKey)).ToList();
    //     var publicEvents = events.Where(e => e.IsPublic()).ToList();
    //     if (publicEvents.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Events",
    //             Members = publicEvents.Select(e => new MemberTableRow
    //             {
    //                 Name = e.ShortSignature(),
    //                 Slug = e.Name + ".html",
    //                 Summary = e.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!e.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //     var nonPublicEvents = events.Where(e => !e.IsPublic()).ToList();
    //     if (nonPublicEvents.Any())
    //     {
    //         memberTables.Add(new MemberTable
    //         {
    //             Title = "Non-Public Events",
    //             CustomCssClasses = ["non-public"],
    //             Members = nonPublicEvents.Select(e => new MemberTableRow
    //             {
    //                 Name = e.ShortSignature(),
    //                 Slug = e.Name + ".html",
    //                 Summary = e.Documentation?.RenderSummary(),
    //                 CustomCssClasses = new[] {!e.IsInheritedFor(typePage) ? "direct" : null!}.Where(c => c != null!).ToList()
    //             }).ToList()
    //         });
    //     }
    //
    //     pge.MemberTables = memberTables.Where(t => !t.IsEmpty).ToList();
    // }
    //
    public static void Collect(Dictionary<string, Action<Context, string>> generators, Context context, NamespaceLayout layout, MemberDocumentation page)
    {
        var slug = page.Slug;
        if (generators.ContainsKey(slug))
        {
            Debug.WriteLine($"Warning: Duplicate generator for slug '{slug}'.");
            return;
        }

        void generate(Context ctx, string s) => Generate(ctx, layout, page);
        generators.Add(slug, generate);
    }
}
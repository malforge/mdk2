using Mdk.DocGen3.Pages.Base;
using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class NamespaceLayout : PageGeneratorLayout<NamespaceModel>
{
    public IEnumerable<TypeItemModel>? Types { get; set; }

    protected MemberPageModelBase Main => GetSubModel<MemberPageModelBase>("main");

    protected override string OnRender() =>
        $"""
         <!DOCTYPE html>
         <html lang="en">
         <head>
             <meta charset="utf-8"/>
             <title>{Esc(Main.Title)}</title>
             <link rel="stylesheet" href="{Esc(Main.CssSlug)}"/>
             <script src="{Esc(Main.JsSlug)}" type="module" defer="defer"></script>
         </head>
         <body>
         <div class="layout">
             <aside class="sidebar">
                 {RenderBasicLinks()}
                 {RenderTypes()}              
             </aside>
             <main class="content">
                 {RenderBody()}
             </main>
         </div>
         </body>
         </html>

         """;

    string RenderBasicLinks() =>
        Join("\n",
            Main.Breadcrumbs?.Select(b => $"""
                                           <div class="type-breadcrumb">
                                               <a href="{b.Slug}">{Esc(b.Name)}</a>
                                           </div>

                                           """)
            ?? []);

    string RenderTypes() =>
        Join("\n",
            Types?.Select(type => $"""
                                   <div class="type-item">
                                       <a href="{type.Slug}">{Esc(type.Name)}</a>
                                       <p>{type.Summary}</p>
                                   </div>

                                   """)
            ?? []);
}
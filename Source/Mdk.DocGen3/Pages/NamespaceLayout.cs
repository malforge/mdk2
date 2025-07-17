using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class NamespaceLayout : MemberPageGenerator
{
    public IEnumerable<TypeItemModel>? Types { get; set; }

    protected override string OnRender() =>
        $"""
         <!DOCTYPE html>
         <html lang="en">
         <head>
             <meta charset="utf-8"/>
             <title>{Esc(this["main"].Title)}</title>
             <link rel="stylesheet" href="{Esc(this["main"].CssSlug)}"/>
             <script src="{Esc(this["main"].JsSlug)}" type="module" defer="defer"></script>
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
            this["main"].Breadcrumbs?.Select(b => $"""
                                                   <div class="type-breadcrumb">
                                                       <a href=\"{b.Slug}\">{Esc(b.Name)}</a>
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
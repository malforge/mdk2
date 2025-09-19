using System.Diagnostics;
using System.Web;
using Mdk.DocGen3.Pages.Base;
using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class ApiIndexPage : PageGenerator<ApiIndexModel>
{
    protected override string OnRender() =>
        $$"""
          <!DOCTYPE html>
          <html lang="en">
          <head>
             <meta charset="utf-8"/>
             <title>{{Model.Title}}</title>
             <link rel="stylesheet" href="{{Model.CssSlug}}"/>
             <link rel="stylesheet" href='@("https://cdn.jsdelivr.net/npm/@docsearch/css@3")'/>
          </head>
          <body>
          <h1>{{Model.Title}}</h1>
          <p>
           Below are all namespaces in this reference. 
           <em>Microsoft namespaces are omitted</em> for brevity and because their docs live on 
           <a href="https://learn.microsoft.com/en-us/dotnet/api/?view=netframework-4.8.1" target="_blank" rel="noopener">Microsoft Learn</a>.
          </p>

          <!-- Algolia DocSearch -->
          <input id="docsearch"
                type="search"
                placeholder="Search the docs…"
                aria-label="Search the docs"/>

          <h2>Namespaces</h2>
          <table>
             {{RenderNamespaces()}}
          </table>

          <script src='@("https://cdn.jsdelivr.net/npm/@docsearch/js@3")'></script>
          <script>
             docsearch({
                 container: '#docsearch',
                 appId: 'YOUR_APP_ID',
                 apiKey: 'YOUR_SEARCH_API_KEY',
                 indexName: 'YOUR_INDEX_NAME'
             });
          </script>
          </body>
          </html>

          """;

    string RenderNamespaces() =>
        Join("\n",
            Model.Namespaces?.Select(ns => $"""
                                      <tr>
                                          <td><a href="{ns.Slug}">{Esc(ns.Title)}</a></td>
                                          <td>{ns.Summary}</td>
                                      </tr>
                                      """)
            ?? []);

    // public static void Generate(Context context)
    // {
    //     var pge = new ApiIndexPage
    //     {
    //         Title = "API Reference",
    //         CssSlug = "css/style.css",
    //         Namespaces = context.Pages
    //             .Where(p => !p.IsExternal() && !p.Namespace.StartsWith("Microsoft."))
    //             .GroupBy(p => p.Namespace)
    //             .OrderBy(g => g.Key)
    //             .Select(g => new TableRow
    //             {
    //                 Title = g.Key,
    //                 Slug = $"{g.Key.Replace('.', '/')}/index.html",
    //                 Summary = context.GetCustomDescription(g.Key)
    //             })
    //             .ToList()
    //     };
    //
    //     context.WriteAllText("index.html", pge.Render());
    //
    //     foreach (var nsGroup in context.Pages
    //                  .Where(p => !p.IsExternal() && !p.Namespace.StartsWith("Microsoft."))
    //                  .GroupBy(p => p.Namespace)
    //                  .OrderBy(g => g.Key))
    //     {
    //         var layout = new NamespaceLayout
    //         {
    //             Types = nsGroup.Select(m => new TypeItemModel
    //             {
    //                 Name = m.Name,
    //                 Slug = m.Slug,
    //                 Summary = m.Documentation?.RenderSummary() ?? string.Empty
    //             }).ToList()
    //         };
    //         NamespaceIndexPage.Generate(context, layout, nsGroup.Key, nsGroup.ToList());
    //     }
    // }

    public static void Collect(Context context, Dictionary<string, Action<Context, string>> generators)
    {
        var slug = "index.html";
        if (generators.ContainsKey(slug))
        {
            Debug.WriteLine($"Warning: Duplicate generator for slug '{slug}'.");
            return;
        }
        
        generators.Add(slug, Generate);
        
        foreach (var nsGroup in context.Pages
                     .Where(p => !p.IsExternal() && !p.Namespace.StartsWith("Microsoft."))
                     .GroupBy(p => p.Namespace)
                     .OrderBy(g => g.Key))
        {
            var layout = new NamespaceLayout
            {
                Types = nsGroup.Select(m => new TypeItemModel
                {
                    Name = m.Name,
                    Slug = m.Slug,
                    Summary = m.Documentation?.RenderSummary() ?? string.Empty
                }).ToList()
            };
            NamespaceIndexPage.Collect(generators, context, layout, nsGroup.Key, nsGroup.ToList());
        }
    }

    static void Generate(Context context, string slug)
    {
        var model = new ApiIndexModel
        {
            Title = "API Reference",
            CssSlug = "css/style.css",
            Namespaces = context.Pages
                .Where(p => !p.IsExternal() && !p.Namespace.StartsWith("Microsoft."))
                .GroupBy(p => p.Namespace)
                .OrderBy(g => g.Key)
                .Select(g => new TableRow
                {
                    Title = g.Key,
                    Slug = $"{HttpUtility.UrlEncode(g.Key)}/index.html",
                    Summary = context.GetCustomDescription(g.Key)
                })
                .ToList()
        };
        
        var pge = new ApiIndexPage
        {
            Model = model
        };

        context.WriteAllText("index.html", pge.Render());
    }
}
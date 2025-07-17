using Mdk.DocGen3.Web;

namespace Mdk.DocGen3.Pages;

public class ApiIndexPage : MemberPageGenerator
{
    public IEnumerable<TableRow>? Namespaces { get; set; }

    protected override string OnRender() =>
        $$"""
          <!DOCTYPE html>
          <html lang="en">
          <head>
             <meta charset="utf-8"/>
             <title>{{Title}}</title>
             <link rel="stylesheet" href="{{CssSlug}}"/>
             <link rel="stylesheet" href='@("https://cdn.jsdelivr.net/npm/@docsearch/css@3")'/>
          </head>
          <body>
          <h1>{{Title}}</h1>
          <p>
           Below are all namespaces in this reference. 
           <em>Microsoft namespaces are omitted</em> for brevity and because their docs live on 
           <a href="https://learn.microsoft.com/en-us/dotnet/api/?view=netframework-4.8.1"target="_blank" rel="noopener">Microsoft Learn</a>.
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
            Namespaces?.Select(ns => $"""
                                      <tr>
                                          <td><a href="{ns.Slug}">{Esc(ns.Title)}</a></td>
                                          <td>{ns.Summary}</td>
                                      </tr>
                                      """)
            ?? []);

    public static void Generate(Context context)
    {
        var pge = new ApiIndexPage
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
                    Slug = $"{g.Key.Replace('.', '/')}/index.html",
                    Summary = context.GetCustomDescription(g.Key)
                })
                .ToList()
        };
        
        context.WriteAllText("index.html", pge.Render());
    }
}
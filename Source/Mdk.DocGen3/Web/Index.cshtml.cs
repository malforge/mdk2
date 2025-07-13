using Mdk.DocGen3.Pages;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Mdk.DocGen3.Web;

public class Index : PageModel
{
    public IEnumerable<NamespaceItemModel>? Namespaces { get; set; }

    public void Generate(Context context)
    {
        Slug = "index.html";
        var engine = context.Engine;
        var namespaceGroups = context.Pages
            .GroupBy(p => p.Namespace)
            .OrderBy(g => g.Key)
            .ToList();

        Namespaces = namespaceGroups.Select(g => new NamespaceItemModel
        {
            Name = g.Key,
            Summary = context.GetCustomDescription(g.Key),
            Slug = $"{g.Key.Replace('.', Path.DirectorySeparatorChar)}/index.html"
        }).ToList();
        
        var result = engine.CompileRenderAsync("Index.cshtml", this).GetAwaiter().GetResult();
        context.WriteHtml("index.html", result);

        foreach (var ns in namespaceGroups)
        {
            var nsModel = new Namespace();
            nsModel.Generate(context, ns.Key, ns);            
        }
    }
}
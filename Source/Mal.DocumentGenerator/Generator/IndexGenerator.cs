using System.IO;
using System.Linq;
using System.Net;

namespace Mal.DocumentGenerator.Generator;

public class IndexGenerator(GeneratorContext context) : Generator
{
    public GeneratorContext Context { get; } = context;

    public override void Generate()
    {
        string categoryName(TypeNode type)
        {
            var name = type.Type.Name;
            while (type.TypeDefinition.IsNested)
            {
                type = type.ParentType!;
                name = type.Type.Name + "." + name;
            }
            return name;
        }

        var types = Context.TypeContext.Types().Where(t => !t.TypeDefinition.IsNested)
            .Select(t => (name: categoryName(t), type: t))
            .OrderBy(t => t.name)
            .GroupBy(t => t.name[0])
            .ToList();

        var template = new BlazorIndexTemplate
        {
            Description = "This page contains an alphabetical index of all types available in the documentation.",
            Categories = types.Select(g => new BlazorIndexTemplate.CategoriesItem
            {
                Name = WebUtility.HtmlEncode(g.Key.ToString()),
                Items = g.Select(t => new BlazorIndexTemplate.ItemsItem
                {
                    Name = WebUtility.HtmlEncode(t.name),
                    Url = $"api/{t.type.Url()}"
                }).ToList()
            }).ToList()
        };

        var fileName = Path.Combine(Context.OutputDirectory, "index.html");
        File.WriteAllText(fileName, template.ToString());
    }
}
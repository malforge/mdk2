using System.Text;
using Mdk.DocGen3.Types;

namespace Mdk.DocGen3.Pages;

public class TypePage(TypeDocumentation typeDocumentation) : Page
{
    public TypeDocumentation TypeDocumentation { get; } = typeDocumentation;

    protected override IMemberDocumentation GetMemberDocumentation() =>
        TypeDocumentation;

    public void Generate(Documentation typeInfo, string modOutputFolder)
    {
        var fileName = Path.GetFullPath(Path.Combine(modOutputFolder, Url));
        Directory.CreateDirectory(Path.GetDirectoryName(fileName) ?? throw new InvalidOperationException("Failed to create directory for type documentation"));
        // Just write an empty file for now

        // The path to the stylesheet is the number of directories in the URL
        var parts = Url.Split('/');
        var stylesheetPath = string.Join("/", Enumerable.Repeat("..", parts.Length)) + "/style.css";

        var fields = TypeDocumentation.Fields.Where(f => f.IsPublic).ToList();
        var constructors = TypeDocumentation.Methods.Where(m => m is { IsConstructor: true, IsPublic: true }).ToList();
        var events = TypeDocumentation.Events.Where(e => e.IsPublic).ToList();
        var properties = TypeDocumentation.Properties.Where(p => p.IsPublic).ToList();
        var methods = TypeDocumentation.Methods.Where(m => m is { IsConstructor: false, IsPublic: true }).ToList();

        var fieldItems = fields.Select(f => new MemberTableTemplate.MembersItem
        {
            // Use an example URL for now
            Url = "https://example.com",
            Name = f.ShortSignature(),
            Description = f.Documentation?.RenderSummary()
        }).ToList();

        var fieldTable = new MemberTableTemplate
        {
            Title = "Fields",
            Members = fieldItems
        };

        var constructorItems = constructors.Select(c => new MemberTableTemplate.MembersItem
        {
            Url = "https://example.com",
            Name = c.ShortSignature(),
            Description = c.Documentation?.RenderSummary()
        }).ToList();

        var constructorTable = new MemberTableTemplate
        {
            Title = "Constructors",
            Members = constructorItems
        };

        var eventItems = events.Select(e => new MemberTableTemplate.MembersItem
        {
            Url = "https://example.com",
            Name = e.ShortSignature(),
            Description = e.Documentation?.RenderSummary()
        }).ToList();

        var eventTable = new MemberTableTemplate
        {
            Title = "Events",
            Members = eventItems
        };

        var propertyItems = properties.Select(p => new MemberTableTemplate.MembersItem
        {
            Url = "https://example.com",
            Name = p.ShortSignature(),
            Description = p.Documentation?.RenderSummary()
        }).ToList();

        var propertyTable = new MemberTableTemplate
        {
            Title = "Properties",
            Members = propertyItems
        };

        var methodItems = methods
            .Where(m => !m.Method.IsSpecialName)
            .Select(m => new MemberTableTemplate.MembersItem
            {
                Url = "https://example.com",
                Name = m.ShortSignature(),
                Description = m.Documentation?.RenderSummary()
            }).ToList();

        var methodTable = new MemberTableTemplate
        {
            Title = "Methods",
            Members = methodItems
        };

        var content = new StringBuilder();
        if (fields.Count > 0)
            content.Append(fieldTable).AppendLine();
        if (constructors.Count > 0)
            content.Append(constructorTable).AppendLine();
        if (events.Count > 0)
            content.Append(eventTable).AppendLine();
        if (properties.Count > 0)
            content.Append(propertyTable).AppendLine();
        if (methods.Count > 0)
            content.Append(methodTable).AppendLine();

        var summary = TypeDocumentation.Documentation?.RenderSummary();
        
        var wrapper = new PageTemplate
        {
            Title = TypeDocumentation.Title,
            StylesheetPath = stylesheetPath,
            Breadcrumbs = "",
            Namespace = TypeDocumentation.Namespace,
            Assembly = TypeDocumentation.Type.Module.Assembly.Name.Name,
            Summary = summary,
            Content = content.ToString(),
            Date = DateTimeOffset.UtcNow.Date.ToString("yyyy-MM-dd")
        };
        File.WriteAllText(fileName, wrapper.ToString());
    }
}
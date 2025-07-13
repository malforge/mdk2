// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using System.Web;
using JetBrains.Annotations;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Pages;
using Mdk.DocGen3.Types;
using Mdk.DocGen3.Web;
using RazorLight;
using Index = Mdk.DocGen3.Web.Index;

namespace Mdk.DocGen3;

public static partial class Program
{
    [UsedImplicitly]
    public static void Main(
        [Description("Target folder for docs")]
        string outputFolder,
        [Switch] [Description("Path to the PB whitelist file")]
        string? pbWhitelist = null,
        [Switch] [Description("Path to the mod whitelist file")]
        string? modWhitelist = null,
        [Switch] [Description("Path to the terminal file")]
        string? terminals = null)
    {
        outputFolder = Path.GetFullPath(outputFolder);
        pbWhitelist ??= "pbwhitelist.dat";
        pbWhitelist = Path.GetFullPath(pbWhitelist);
        modWhitelist ??= "modwhitelist.dat";
        modWhitelist = Path.GetFullPath(modWhitelist);
        terminals ??= "terminals.dat";
        terminals = Path.GetFullPath(terminals);

        Console.WriteLine($"Output folder: {outputFolder}");
        Console.WriteLine($"PB whitelist: {pbWhitelist}");
        Console.WriteLine($"Mod whitelist: {modWhitelist}");
        Console.WriteLine($"Terminals: {terminals}");

        var pbWhitelistInstance = Whitelist.Load(pbWhitelist);
        var modWhitelistInstance = Whitelist.Load(modWhitelist);
        var terminalsInstance = Terminals.Load(terminals);

        var typeInfo = TypeLoader.LoadTypeInfo(@"C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64\", pbWhitelistInstance);

        var modOutputFolder = Path.Combine(outputFolder, "Mods");
        var pbOutputFolder = Path.Combine(outputFolder, "ProgrammableBlocks");

        // If the mod output folder exists, delete it
        if (Directory.Exists(modOutputFolder))
        {
            Console.WriteLine($"Deleting existing mod output folder: {modOutputFolder}");
            Directory.Delete(modOutputFolder, true);
        }

        // If the PB output folder exists, delete it
        if (Directory.Exists(pbOutputFolder))
        {
            Console.WriteLine($"Deleting existing PB output folder: {pbOutputFolder}");
            Directory.Delete(pbOutputFolder, true);
        }

        Console.Write("Generating... ");

        // generate a list of Pages.Page from the types.
        var pages = typeInfo.Types.SelectMany(ResolvePages).ToList();

        var consolePos = (Console.CursorLeft, Console.CursorTop);
        var n = 0;
        var maxText = $"100% ({pages.Count}/{pages.Count})";
        var maxTextLength = maxText.Length;
        var skippedCount = 0;
        var generatedCount = 0;

        // // For the sake of testing: Find a page that has a representation of every type of member
        // // and generate it alone
        //  var testPage = pages.OfType<TypePage>()
        //      .FirstOrDefault(p => p.TypeDocumentation.Fields.Count > 0 && 
        //                           p.TypeDocumentation.Properties.Count > 0 && 
        //                           p.TypeDocumentation.Methods.Count > 0 && 
        //                           p.TypeDocumentation.Events.Count > 0 /*&&
        //                           p.TypeDocumentation.NestedTypes.Count > 0*/);
        //
        // Find the typepage for "MyFueledPowerProducer" to use as a test page
        // var testPage = pages.OfType<TypePage>()
        //     .FirstOrDefault(p => p.Url.EndsWith("TextPtr_struct.html", StringComparison.OrdinalIgnoreCase));
        //
        // Directory.CreateDirectory(modOutputFolder);
        // StyleSheet.Write(Path.Combine(modOutputFolder, "style.css"));
        // testPage.Generate(typeInfo, modOutputFolder);

        Directory.CreateDirectory(modOutputFolder);
        Directory.CreateDirectory(pbOutputFolder);
        List<DocumentationPage> pbPages = new List<DocumentationPage>();
        List<DocumentationPage> modPages = new List<DocumentationPage>();

        foreach (var page in pages)
        {
            n++;
            if (n % 100 == 0)
            {
                var pct = n * 100 / pages.Count;
                Console.SetCursorPosition(consolePos.Item1, consolePos.Item2);
                var text = $"{pct}% ({n}/{pages.Count})".PadRight(maxTextLength);
                Console.Write(text);
            }
            bool wasGenerated = false;
            if (!page.IsIgnored(modWhitelistInstance))
            {
                // Add this only if it's not a nested type
                if (page is TypePage {TypeDocumentation.Type.IsNested: false})
                    modPages.Add(page);
            }
            if (!page.IsIgnored(pbWhitelistInstance))
            {
                if (page is TypePage {TypeDocumentation.Type.IsNested: false})
                    pbPages.Add(page);
            }
            //
            // if (page.IsMicrosoftType())
            // {
            //     skippedCount++;
            //     continue; // Skip Microsoft types
            // }
            //
            // var wasGenerated = false;
            // if (page.IsWhitelisted(modWhitelistInstance))
            // {
            //     page.Generate(typeInfo, modOutputFolder);
            //     wasGenerated = true;
            // }
            // if (page.IsWhitelisted(pbWhitelistInstance))
            // {
            //     page.Generate(typeInfo, pbOutputFolder);
            //     wasGenerated = true;
            // }

            if (wasGenerated)
            {
                generatedCount++;
            }
            else
            {
                skippedCount++;
            }
        }
        Console.SetCursorPosition(consolePos.Item1, consolePos.Item2);
        Console.WriteLine($"100% ({pages.Count}/{pages.Count})");
        Console.WriteLine($"Generated {generatedCount} pages, skipped {skippedCount} pages.");

        // Sort the modPages and pbPages by their type name
        modPages.Sort((a, b) =>
            string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        pbPages.Sort((a, b) =>
            string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Web"))
            .UseMemoryCachingProvider()
            .Build();

        // Copy style.css to the output folder /css
        
        // var context = new Context(engine, modOutputFolder, modPages);
        // var cssFile = Path.Combine(modOutputFolder, "css", "style.css");
        // Directory.CreateDirectory(Path.GetDirectoryName(cssFile)!);
        // File.Copy(Path.Combine("Web", "style.css"), cssFile, true);
        // var index = new Index();
        // index.Generate(context);
        
        GenerateApiDocumentation(engine, modOutputFolder, modPages);
        GenerateApiDocumentation(engine, pbOutputFolder, pbPages);
    }

    static void GenerateApiDocumentation(RazorLightEngine engine, string pbOutputFolder, List<DocumentationPage> pbPages)
    {
        var index = new Index();
        var context = new Context(engine, pbOutputFolder, pbPages);
        var cssFile = Path.Combine(pbOutputFolder, "css", "style.css");
        Directory.CreateDirectory(Path.GetDirectoryName(cssFile)!);
        File.Copy(Path.Combine("Web", "style.css"), cssFile, true);
        index.Generate(context);
    }

    static IEnumerable<DocumentationPage> ResolvePages(TypeDocumentation td)
    {
        var page = new TypePage(td);
        yield return page;
        foreach (var field in td.Fields)
            yield return new FieldPage(field);
        foreach (var property in td.Properties)
            yield return new PropertyPage(property);
        var methodsByName = td.Methods.GroupBy(m => m.Name).ToList();
        if (methodsByName.Count > 0)
        {
            foreach (var methodGroup in methodsByName)
                yield return new MethodPage(methodGroup);
        }
        foreach (var eventDef in td.Events)
            yield return new EventPage(eventDef);
        foreach (var nestedType in td.NestedTypes)
        foreach (var nestedPage in ResolvePages(nestedType))
            yield return nestedPage;
    }

    static string ToValidFileName(string docKey) =>
        // Replace invalid characters with underscores
        string.Concat(docKey.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
}
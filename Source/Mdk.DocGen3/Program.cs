// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using JetBrains.Annotations;
using Mdk.DocGen3.CodeDoc;
using Mdk.DocGen3.CodeSecurity;
using Mdk.DocGen3.Pages;
using Mdk.DocGen3.Types;
using RazorLight;

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

        // Install SteamCmd and the game under the running program's directory
        var steamCmdInstallPath = Path.Combine(Directory.GetCurrentDirectory(), "SteamCmd");
        var gameInstallPath = Path.Combine(Directory.GetCurrentDirectory(), "SpaceEngineersDedicated");
        SteamCmd.Instance.DownloadOrUpdateGame(steamCmdInstallPath, gameInstallPath).GetAwaiter().GetResult();

        var context = new TypeLoadingContext(Path.Combine(gameInstallPath, "DedicatedServer64"),
            type =>
            {
                return true;
                // // Passes either the mod- or pb-whitelist key to the type.
                // var docKey = Doc.GetDocKey(type);
                // return pbWhitelistInstance.IsAllowed(docKey) || modWhitelistInstance.IsAllowed(docKey);
            });
        
        var typeInfo = TypeLoader.LoadTypeInfo(context);

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
        var pages = typeInfo.Types;

        // Copy index.html and index.css to the main output folder
        var indexHtml = Path.Combine(outputFolder, "index.html");
        var indexCss = Path.Combine(outputFolder, "index.css");
        Directory.CreateDirectory(Path.GetDirectoryName(indexHtml)!);
        Directory.CreateDirectory(Path.GetDirectoryName(indexCss)!);
        File.Copy(Path.Combine("Web", "boot.html"), indexHtml, true);
        File.Copy(Path.Combine("Web", "boot.css"), indexCss, true);

        Directory.CreateDirectory(modOutputFolder);
        Directory.CreateDirectory(pbOutputFolder);
        var pbPages = new List<MemberDocumentation>();
        var modPages = new List<MemberDocumentation>();

        foreach (var page in pages)
        {
            if (modWhitelistInstance.IsAllowed(page.WhitelistKey))
            {
                if (!page.IsNested)
                    modPages.Add(page);
            }
            if (pbWhitelistInstance.IsAllowed(page.WhitelistKey))
            {
                if (!page.IsNested)
                    pbPages.Add(page);
            }
        }
        // Sort the modPages and pbPages by their type name
        modPages.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        pbPages.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

        var engine = new RazorLightEngineBuilder()
            .UseFileSystemProject(Path.Combine(Directory.GetCurrentDirectory(), "Web"))
            .UseMemoryCachingProvider()
            .Build();

        // GenerateApiDocumentation("Mod API", "Mods", engine, modOutputFolder, modWhitelistInstance, modPages);
        GenerateApiDocumentation("Programmable Block API", "ProgrammableBlocks", engine, pbOutputFolder, pbWhitelistInstance, pbPages);
    }

    static void GenerateApiDocumentation(string name, string rootSlug, RazorLightEngine engine, string pbOutputFolder, Whitelist whitelist, List<MemberDocumentation> pbPages)
    {
        // var index = new NamespaceIndexPage();
        // var index = new Index();
        var context = new Context(name, rootSlug, engine, whitelist, pbOutputFolder, pbPages);
        var cssFile = Path.Combine(pbOutputFolder, "css", "style.css");
        var jsFile = Path.Combine(pbOutputFolder, "js", "script.js");
        var jsMapFile = Path.Combine(pbOutputFolder, "js", "script.js.map");
        Directory.CreateDirectory(Path.GetDirectoryName(cssFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(jsFile)!);
        File.Copy(Path.Combine("Web", "style.css"), cssFile, true);
        File.Copy(Path.Combine("Web", "script.js"), jsFile, true);
        File.Copy(Path.Combine("Web", "script.js.map"), jsMapFile, true);

        Dictionary<string, Action<Context, string>> generators = new();
        ApiIndexPage.Collect(context, generators);
        
        foreach (var (slug, generate) in generators)
        {
            generate(context, slug);
        }
        
        
        //ApiIndexPage.Generate(context);
        // index.Generate(context);
    }

    // static IEnumerable<DocumentationPage> ResolvePages(TypeDocumentation td)
    // {
    //     var page = new TypePage(td);
    //     yield return page;
    //     foreach (var field in td.Fields)
    //         yield return new FieldPage(field);
    //     foreach (var property in td.Properties)
    //         yield return new PropertyPage(property);
    //     var methodsByName = td.Methods.GroupBy(m => m.Name).ToList();
    //     if (methodsByName.Count > 0)
    //     {
    //         foreach (var methodGroup in methodsByName)
    //             yield return new MethodPage(methodGroup);
    //     }
    //     foreach (var eventDef in td.Events)
    //         yield return new EventPage(eventDef);
    //     foreach (var nestedType in td.NestedTypes)
    //     foreach (var nestedPage in ResolvePages(nestedType))
    //         yield return nestedPage;
    // }

    static string ToValidFileName(string docKey) =>
        // Replace invalid characters with underscores
        string.Concat(docKey.Split(Path.GetInvalidFileNameChars())).Replace(' ', '_');
}
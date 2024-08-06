using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Mal.DocumentGenerator;
using Mal.DocumentGenerator.Common;
using Mal.DocumentGenerator.DocExtensions;
using Mal.DocumentGenerator.Generator;
using Mal.DocumentGenerator.Whitelists;
using Mono.Cecil;
using Mono.Cecil.Rocks;
var parameters = new Parameters();

try
{
    parameters.LoadFromArgs(args);
    if (parameters.Help)
    {
        ConfigObject.GetDefinition(parameters).WriteCommandLineUsage(Console.Out);
        return 0;
    }

    if (parameters.Path != null && !Directory.Exists(parameters.Path))
        throw new CommandLineException("Path does not exist");

    if (!File.Exists(parameters.PbWhitelist))
        throw new CommandLineException("PB Whitelist does not exist");

    if (!File.Exists(parameters.ModWhitelist))
        throw new CommandLineException("Mod Whitelist does not exist");

    if (!File.Exists(parameters.Terminal))
        throw new CommandLineException("Terminal does not exist");

    var se = new SpaceEngineers();
    var sePath = se.GetInstallPath("Bin64");

    if (string.IsNullOrEmpty(sePath) || !Directory.Exists(sePath))
        throw new CommandLineException($"Cannot find designated SE path \"{sePath}\"");


    //ReflectionAssemblyManager.Init(sePath);
    var outputPath = parameters.Output ?? Path.Combine(Directory.GetCurrentDirectory(), "output");
    if (!Directory.Exists(outputPath))
        Directory.CreateDirectory(outputPath);

    var pbWhitelist = Whitelist.Load(parameters.PbWhitelist);
    var spaceTextRule = pbWhitelist.WhitelistRules.Where(r => r.Path.Contains("MySpaceTexts")).ToList();
    pbWhitelist.AddBlacklist("Sandbox.Game.Localization.MySpaceTexts+*, Sandbox.Game");
    var modWhitelist = Whitelist.Load(parameters.ModWhitelist);
    var terminal = Terminals.Load(parameters.Terminal);

    Summary[] extensions =
    [
        new Summary
        {
            Target = "Sandbox.Game.Localization.MySpaceTexts",
            AuthorNickname = "Malware",
            AuthorUserId = "mlyrstad@gmail.com",
            Date = DateTimeOffset.Now,
            ReplacesOriginal = false,
            SummaryText =
                """
                The MySpaceTexts class holds the localization strings for Space Engineers. There are many thousands of strings in this class, so we will not be documenting them all here.
                If you need to find the properties, you may be able to use your IDE's autocomplete feature to find the property you are looking for, depending
                on the IDE you are using. If you are using Visual Studio, you can press Ctrl+Space to bring up the autocomplete menu. 
                """
        }
    ];

    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(sePath);
    var readerParameters = new ReaderParameters { AssemblyResolver = resolver };

    var context = new TypeContextBuilder(sePath);
    var asm = AssemblyDefinition.ReadAssembly(Path.Combine(sePath, "SpaceEngineers.exe"), readerParameters);
    var visitor = new DocumentGeneratorVisitor(pbWhitelist, context, asm);
    visitor.Visit();

    var extensionMethods = context.Everything().OfType<MethodNode>().Where(m => m.IsExtensionMethod()).ToList();

    foreach (var method in extensionMethods)
    {
        foreach (var type in method.ExtensionTargets())
            type.AddExtensionMethod(method);
    }

    context.Close();
    
    var generatorContext = new GeneratorContext(context, outputPath);

    List<Generator> generators = [new IndexGenerator(generatorContext)];
    generators.AddRange(context.Types().Select(t => new TypeGenerator(generatorContext, t)));

    // Parallel.ForEach(generators, g => g.Generate());
    foreach (var generator in generators)
        generator.Generate();

    return 0;
}
catch (CommandLineException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    ConfigObject.GetDefinition(parameters).WriteCommandLineUsage(Console.Out);
    return -1;
}

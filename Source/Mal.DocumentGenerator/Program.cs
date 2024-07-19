using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mal.DocumentGenerator;
using Mal.DocumentGenerator.Common;
using Mal.DocumentGenerator.DocExtensions;
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

    var se = new Mal.DocumentGenerator.Common.SpaceEngineers();
    var sePath = se.GetInstallPath("Bin64");

    if (string.IsNullOrEmpty(sePath) || !Directory.Exists(sePath))
        throw new CommandLineException($"Cannot find designated SE path \"{sePath}\"");

    //ReflectionAssemblyManager.Init(sePath);

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

    var context = new Context(sePath);
    var asm = AssemblyDefinition.ReadAssembly(Path.Combine(sePath, "SpaceEngineers.exe"), readerParameters);
    var visitor = new DocumentGeneratorVisitor(pbWhitelist, context, asm);
    visitor.Visit();
    
    var everything = context.Everything()/*.Where(n => n is TypeNode)*/.ToList();
    
    // var entries = new List<string>();
    // //
    //  var visited = new HashSet<AssemblyDefinition>();
    // visitAssemblyForPb(asm, readerParameters, pbWhitelist, visited, entries);
    //
    //
    // // E:\Repos\SpaceEngineers\MDK-SE.wiki\api
    // var apiPath = "E:\\Repos\\SpaceEngineers\\MDK-SE.wiki\\api";
    // char[] badChars = ['<', '('];
    //
    // foreach (var entry in entries)
    // {
    //     var stopIndex = entry.IndexOfAny(badChars);
    //     var safeEntry = stopIndex == -1 ? entry : entry.Substring(0, stopIndex);
    //     File.WriteAllText(Path.Combine(apiPath, safeEntry + ".txt"), "");
    // }
    //
    // foreach (var entry in entries)
    //     Console.WriteLine(entry);
    // Console.WriteLine("Found {0} entries", entries.Count);
    //
    // // Write entries to a file
    // File.WriteAllLines("pb-entries.txt", entries);
    // // Open the folder in Explorer
    // var folder = Path.GetDirectoryName(Path.GetFullPath("pb-entries.txt"));
    // Process.Start("explorer.exe", folder);

    return 0;
}
catch (CommandLineException ex)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    ConfigObject.GetDefinition(parameters).WriteCommandLineUsage(Console.Out);
    return -1;
}

void visitAssemblyForPb(AssemblyDefinition assembly, ReaderParameters rp, Whitelist whitelist, HashSet<AssemblyDefinition> visited, List<string> entries)
{
    if (!visited.Add(assembly))
        return;

    if (assembly.IsMicrosoftAssembly())
        return;

    var module = assembly.MainModule;
    var types = module.Types;
    foreach (var type in types)
    {
        if (type.IsSpecialName || type.IsRuntimeSpecialName)
            continue;
        if (type is {IsPublic: false, IsNestedPublic: false})
            continue;

        var fullNameOfType = type.GetWhitelistName();
        if (!whitelist.IsWhitelisted(assembly.Name.Name, fullNameOfType))
            continue;

        entries.Add(fullNameOfType);

        foreach (var member in type.GetConstructors())
        {
            var fullName = member.GetWhitelistName();
            if (!whitelist.IsWhitelisted(assembly.Name.Name, fullName))
                continue;

            if (type.IsSealed && !member.IsPublic)
                continue;
            if (member.IsPrivate)
                continue;

            entries.Add(member.GetWhitelistName());
        }

        foreach (var member in type.Fields)
        {
            if (member.IsSpecialName || member.IsRuntimeSpecialName)
                continue;

            if (type.IsSealed && !member.IsPublic)
                continue;
            if (member.IsPrivate)
                continue;

            var fullName = member.GetWhitelistName();
            if (!whitelist.IsWhitelisted(assembly.Name.Name, fullName))
                continue;

            entries.Add(fullName);
        }

        foreach (var member in type.Properties)
        {
            if (member.IsSpecialName || member.IsRuntimeSpecialName)
                continue;

            if (type.IsSealed && !(member.GetMethod?.IsPublic ?? true) && !(member.SetMethod?.IsPublic ?? true))
                continue;
            if ((member.GetMethod?.IsPrivate ?? true) && (member.SetMethod?.IsPrivate ?? true))
                continue;

            var fullName = member.GetWhitelistName();
            if (!whitelist.IsWhitelisted(assembly.Name.Name, fullName))
                continue;

            entries.Add(fullName);
        }

        foreach (var member in type.Events)
        {
            if (member.IsSpecialName || member.IsRuntimeSpecialName)
                continue;

            if (type.IsSealed && !member.AddMethod.IsPublic && !member.RemoveMethod.IsPublic)
                continue;
            if (member.AddMethod.IsPrivate && member.RemoveMethod.IsPrivate)
                continue;

            var fullName = member.GetWhitelistName();
            if (!whitelist.IsWhitelisted(assembly.Name.Name, fullName))
                continue;

            entries.Add(fullName);
        }

        // foreach (var member in type.NestedTypes)
        // {
        //     if (!whitelist.IsWhitelisted(assembly.Name.Name, GetFullNameOf(member)))
        //         continue;
        //
        //     Console.WriteLine($"{type.FullName}.{member.Name}");
        // }

        foreach (var member in type.Methods)
        {
            if (member.IsSpecialName || member.IsRuntimeSpecialName)
                continue;
            if (type.IsSealed && !member.IsPublic)
                continue;
            if (member.IsPrivate)
                continue;

            var fullName = member.GetWhitelistName();
            if (!whitelist.IsWhitelisted(assembly.Name.Name, fullName))
                continue;

            entries.Add(fullName);
        }
    }

    foreach (var subassembly in module.AssemblyReferences)
    {
        if (!whitelist.IsAssemblyWhitelisted(subassembly.Name))
            continue;

        // Resolve the assembly
        var asm = rp.AssemblyResolver.Resolve(subassembly);
        visitAssemblyForPb(asm, rp, whitelist, visited, entries);
    }
}
// See https://aka.ms/new-console-template for more information

using System.ComponentModel;
using JetBrains.Annotations;

namespace Mdk.DocGen2;

partial class Program
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
        Console.WriteLine("Hello, World!");
    }
}
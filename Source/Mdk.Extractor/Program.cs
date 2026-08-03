using System;
using System.IO;
#pragma warning disable CS0028

namespace Mdk.Extractor;

public partial class Program
{
    [Verb, Default]
    public static void Main([Switch] string modWhitelist = null, [Switch] string pbWhitelist = null, [Switch] string pbPrologue = null, [Switch] string terminal = null, [Switch] string sePath = null)
    {
        modWhitelist ??= "modwhitelist.dat";
        pbWhitelist ??= "pbwhitelist.dat";
        pbPrologue ??= "pbprologue.dat";
        terminal ??= "terminal.dat";

        modWhitelist = Path.GetFullPath(modWhitelist);
        pbWhitelist = Path.GetFullPath(pbWhitelist);
        pbPrologue = Path.GetFullPath(pbPrologue);
        terminal = Path.GetFullPath(terminal);
        
        var se = new SpaceEngineers();
        sePath ??= se.GetInstallPath("Bin64");

        if (string.IsNullOrEmpty(sePath) || !Directory.Exists(sePath))
            throw new TerminalException($"Cannot find designated SE path \"{sePath}\"");

        var program = new Extractor(modWhitelist, pbWhitelist, pbPrologue, terminal, sePath);
        var oldPath = Environment.CurrentDirectory;
        Environment.CurrentDirectory = sePath;
        try
        {
            program.Run();
        }
        finally
        {
            Environment.CurrentDirectory = oldPath;
        }
    }
}
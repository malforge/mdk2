using System;
using System.IO;
#pragma warning disable CS0028

namespace Mdk.Extractor;

public partial class Program
{
    public static void Main(string modWhitelist = null, string pbWhitelist = null, string terminal = null, string sePath = null)
    {
        modWhitelist ??= "modwhitelist.dat";
        pbWhitelist ??= "pbwhitelist.dat";
        terminal ??= "terminal.dat";

        modWhitelist = Path.GetFullPath(modWhitelist);
        pbWhitelist = Path.GetFullPath(pbWhitelist);
        terminal = Path.GetFullPath(terminal);
        
        var se = new SpaceEngineers();
        sePath ??= se.GetInstallPath("Bin64");

        if (string.IsNullOrEmpty(sePath) || !Directory.Exists(sePath))
            throw new TerminalException($"Cannot find designated SE path \"{sePath}\"");

        var program = new Extractor(modWhitelist, pbWhitelist, terminal, sePath);
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
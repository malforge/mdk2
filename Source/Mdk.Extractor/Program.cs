using System;
using System.IO;
#pragma warning disable CS0028

namespace Mdk.Extractor;

public partial class Program
{
    [Verb, Default]
    public static void Main([Switch] string modWhitelist = null, [Switch] string pbWhitelist = null, [Switch] string pbPrologue = null, [Switch] string terminal = null,
        [Switch] string sePath = null, [Switch] string instance = null, [Switch] string timeout = null)
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
        sePath ??= se.GetDedicatedServerPath();

        if (string.IsNullOrEmpty(sePath) || !Directory.Exists(sePath))
            throw new TerminalException($"Cannot find the Space Engineers dedicated server. Pass -sePath <DedicatedServer64 folder>, or set the {SpaceEngineers.BinPathVariable} environment variable. Looked for: \"{sePath}\"");

        sePath = Path.GetFullPath(sePath);

        // Kept outside the output folder: the server treats this as its own data directory and fills it with
        // saves, caches and logs that have nothing to do with what we are extracting.
        instance ??= Path.Combine(Path.GetTempPath(), "MdkExtractorInstance");
        instance = Path.GetFullPath(instance);

        var timeoutSpan = TimeSpan.FromMinutes(10);
        if (!string.IsNullOrEmpty(timeout))
        {
            if (!int.TryParse(timeout, out var seconds) || seconds <= 0)
                throw new TerminalException($"\"{timeout}\" is not a valid timeout in seconds");
            timeoutSpan = TimeSpan.FromSeconds(seconds);
        }

        var program = new Extractor(modWhitelist, pbWhitelist, pbPrologue, terminal, sePath, instance, timeoutSpan);
        program.Run();

        Console.WriteLine(@"Extraction complete:");
        Console.WriteLine($@"  {modWhitelist}");
        Console.WriteLine($@"  {pbWhitelist}");
        Console.WriteLine($@"  {pbPrologue}");
        Console.WriteLine($@"  {terminal}");
    }
}

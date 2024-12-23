using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Mod.Restore;

/// <summary>
/// A fix for the issue causing the .ini ignores files to use ; instead of , as a separator. 
/// </summary>
public static partial class BadIgnoresBugFix
{
    /// <summary>
    /// Checks the project file for bad ignores and fixes them.
    /// </summary>
    /// <param name="projectFileName"></param>
    /// <param name="console"></param>
    public static async Task CheckAsync(string projectFileName, IConsole console)
    {
        var iniFileName = Path.ChangeExtension(projectFileName, ".mdk.ini");
        if (!File.Exists(iniFileName))
            return;

        try
        {
            var iniContent = await File.ReadAllLinesAsync(iniFileName);
            var regex = BadLineRegex();
            for (var index = 0; index < iniContent.Length; index++)
            {
                var line = iniContent[index];
                var match = regex.Match(line);
                if (!match.Success)
                    continue;

                var ignores = match.Groups[1].Value;
                var fixedIgnores = ignores.Replace(';', ',');
                iniContent[index] = line.Replace(ignores, fixedIgnores);
            }
            await File.WriteAllLinesAsync(iniFileName, iniContent);
        }
        catch (Exception e)
        {
            console.Print($"Failed to fix bad ignores in .ini file: {e.Message}");
        }
    }

    [GeneratedRegex("ignores=(.+)(;.+)+", RegexOptions.Singleline)]
    private static partial Regex BadLineRegex();
}
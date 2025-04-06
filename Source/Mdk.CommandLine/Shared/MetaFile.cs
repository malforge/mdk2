using System.IO;
using System.Threading.Tasks;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine.Shared;

/// <summary>
///     The meta file is used to identify a folder as part of an MDK project. This class provides methods to create and
///     check for the existence of the meta file.
/// </summary>
public static class MetaFile
{
    const string MetaFileContent = """
                                   About this file
                                   ===============

                                   This file, `mdk.meta`, is used by MDK tools to identify this folder as part of 
                                   your {0} project. It’s here to help manage the folder during builds 
                                   and updates.

                                   Why is this here?
                                   -----------------
                                   When MDK packs or updates your {0}, it uses this file to confirm that 
                                   it’s working in the right place. It’s a simple way to keep things organized and 
                                   running smoothly.

                                   Does this affect my {0}?
                                   ------------------------
                                   Not at all! This file is only used by the tools during the build process. It 
                                   has no effect on the {0} itself or how it runs in the game.

                                   Can I delete it?
                                   ----------------
                                   If you’re no longer using this folder for your {0}, you can delete it. 
                                   Otherwise, leaving it here ensures everything works as expected during builds.
                                   """;

    /// <summary>
    ///     Writes the meta file to the specified file system.
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="projectType"></param>
    public static async Task WriteAsync(IFileSystem fileSystem, MdkProjectType projectType)
    {
        var type = projectType switch
        {
            MdkProjectType.ProgrammableBlock => "script",
            MdkProjectType.Mod => "mod",
            MdkProjectType.LegacyProgrammableBlock => "script",
            _ => "unknown"
        };
        
        await fileSystem.WriteAsync("mdk.meta", string.Format(MetaFileContent, type));
    }

    /// <summary>
    ///     Determines if the specified file system has a meta file.
    /// </summary>
    /// <param name="fileSystem"></param>
    /// <param name="subFolder"></param>
    /// <returns></returns>
    public static bool HasMetaFile(this IFileSystem fileSystem, string subFolder)
    {
        var path = Path.Combine(subFolder, "mdk.meta");
        return fileSystem.Exists(path);
    }
}
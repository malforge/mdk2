using System;
using System.IO;

namespace Mdk.Extractor
{
    /// <summary>
    /// Utility service to locate the Space Engineers dedicated server (copyright Keen Software House, no affiliation)
    /// </summary>
    /// <remarks>
    /// The extractor runs against the dedicated server rather than the game. The dedicated server is a separate
    /// Steam application (298740) which can be installed anonymously through steamcmd, so it is often not
    /// registered with the Steam client at all. The environment variable is therefore the primary route and the
    /// Steam lookup is a convenience for machines where it was installed through the client.
    /// </remarks>
    class SpaceEngineers
    {
        /// <summary>
        /// Environment variable naming the DedicatedServer64 folder directly.
        /// </summary>
        public const string BinPathVariable = "MDK_SE_DEDICATED_BIN";

        /// <summary>
        /// The Steam App ID of the Space Engineers Dedicated Server
        /// </summary>
        public const long SteamAppId = 298740;

        const string InstallFolderName = "SpaceEngineersDedicatedServer";
        const string BinFolderName = "DedicatedServer64";
        const string Executable = "SpaceEngineersDedicated.exe";

        /// <summary>
        /// The <see cref="Steam"/> service
        /// </summary>
        public Steam Steam { get; } = new Steam();

        /// <summary>
        /// Attempts to locate the dedicated server's binary folder.
        /// </summary>
        /// <returns>The full path to a DedicatedServer64 folder, or <c>null</c> if none could be found.</returns>
        public string GetDedicatedServerPath()
        {
            var fromEnvironment = Environment.GetEnvironmentVariable(BinPathVariable);
            if (!string.IsNullOrEmpty(fromEnvironment) && File.Exists(Path.Combine(fromEnvironment, Executable)))
                return Path.GetFullPath(fromEnvironment);

            if (!Steam.Exists)
                return null;

            var installFolder = Steam.GetInstallFolder(InstallFolderName, Path.Combine(BinFolderName, Executable));
            if (string.IsNullOrEmpty(installFolder))
                return null;

            return Path.GetFullPath(Path.Combine(installFolder, BinFolderName));
        }
    }
}

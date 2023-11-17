using System;
using System.IO;

namespace Mdk2.References.Utility
{
    /// <summary>
    ///     Utility service to retrieve information about Space Engineers (copyright Keen Software House, no affiliation)
    /// </summary>
    class SpaceEngineers
    {
        /// <summary>
        ///     The Steam App ID of Space Engineers
        /// </summary>
        public const long SteamAppId = 244850;

        /// <summary>
        ///     The <see cref="Steam" /> service
        /// </summary>
        public Steam Steam { get; } = new Steam();

        /// <summary>
        ///     Attempts to get the install path of Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <param name="installPath"></param>
        /// <returns></returns>
        public bool TryGetInstallPath(string subfolders, out string installPath)
        {
            installPath = default;
            if (!Steam.Exists)
                return false;
            var installFolder = Steam.GetInstallFolder("SpaceEngineers", "Bin64\\SpaceEngineers.exe");
            if (string.IsNullOrEmpty(installFolder))
                return false;
            if (string.IsNullOrEmpty(subfolders))
                installPath = Path.GetFullPath(installFolder);
            else
                installPath = Path.GetFullPath(Path.Combine(installFolder, subfolders));
            return true;
        }

        /// <summary>
        ///     Attempts to get the install path of Space Engineers.
        /// </summary>
        /// <param name="installPath"></param>
        /// <returns></returns>
        public bool TryGetInstallPath(out string installPath)
        {
            installPath = default;
            if (!Steam.Exists)
                return false;
            var installFolder = Steam.GetInstallFolder("SpaceEngineers", "Bin64\\SpaceEngineers.exe");
            if (string.IsNullOrEmpty(installFolder))
                return false;
            installPath = Path.GetFullPath(installFolder);
            return true;
        }

        /// <summary>
        ///     Attempts to get the default data path for Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        public string GetDataPath(string subfolders = null)
        {
            var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers");
            if (string.IsNullOrEmpty(subfolders))
                return Path.GetFullPath(dataFolder);
            return Path.GetFullPath(Path.Combine(dataFolder, subfolders));
        }
    }
}
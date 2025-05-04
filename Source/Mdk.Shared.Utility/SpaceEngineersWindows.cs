using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Mdk2.References.Utility;

namespace Mdk2.Shared.Utility
{
    /// <summary>
    /// Utility service to retrieve information about Space Engineers (copyright Keen Software House, no affiliation)
    /// </summary>
    class SpaceEngineersWindows : ISpaceEngineers
    {
        /// <summary>
        /// The <see cref="Steam"/> service
        /// </summary>
        public Steam Steam { get; } = new Steam();

        /// <summary>
        /// The Steam App ID of Space Engineers
        /// </summary>
        public const long SteamAppId = 244850;

        /// <summary>
        /// Attempts to get the install path of Space Engineers.
        /// </summary>
        /// <returns></returns>
        public string GetInstallPath() => GetInstallPath(Array.Empty<string>());

        /// <summary>
        /// Attempts to get the install path of Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        public string GetInstallPath(params string[] subfolders)
        {
            if (!Steam.Exists)
                throw new Exception("Steam doesn't exist");
            var installFolder = Steam.GetInstallFolder("SpaceEngineers", "Bin64\\SpaceEngineers.exe");
            if (string.IsNullOrEmpty(installFolder))
                throw new Exception("Steam isn't installed");
            if (subfolders.Length == 0)
                return Path.GetFullPath(installFolder);

            subfolders = new[] {installFolder}.Concat(subfolders).ToArray();
            return Path.GetFullPath(Path.Combine(subfolders));
        }

        /// <summary>
        /// Attempts to get the default data path for Space Engineers.
        /// </summary>
        /// <returns></returns>
        public string GetDataPath() => GetDataPath(Array.Empty<string>());

        /// <summary>
        /// Attempts to get the default data path for Space Engineers.
        /// </summary>
        /// <param name="subfolders">The desired subfolder path, if any</param>
        /// <returns></returns>
        public string GetDataPath(params string[] subfolders)
        {
            var dataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpaceEngineers");
            if (subfolders.Length <= 0)
                return Path.GetFullPath(dataFolder);

            subfolders = new[] {dataFolder}.Concat(subfolders).ToArray();
            return Path.GetFullPath(Path.Combine(subfolders));
        }
    }
}

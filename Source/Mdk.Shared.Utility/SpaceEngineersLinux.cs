using System;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace Mdk2.Shared.Utility
{
    internal class SpaceEngineersLinux : ISpaceEngineers
    {
        public string GetInstallPath() => GetInstallPath(new string[] { });

        public string GetInstallPath(params string[] subfolders) => FindStartAtHome("SpaceEngineers/Bin64");

        public string GetDataPath(params string[] subfolders) => FindStartAtHome("SpaceEngineers/IngameScripts/local");

        private static string FindStartAtHome(string findPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "find",
                Arguments = $"/home -path \"*/{findPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using (var process = Process.Start(startInfo))
            {
                if (process == null) throw new Exception("Failed to find IngameScripts folder.");
                process.WaitForExit();
                return process.StandardOutput.ReadToEnd().Trim();
            }
        }
    }
}
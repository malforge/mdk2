// Mdk.References
// 
// Copyright 2023 Morten A. Lyrstad

using System.Runtime.InteropServices;
using Mdk2.References.Utility;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Mdk2.References
{
    public class SpaceEngineersFinder : Task
    {
        [Output]
        public string DataPath { get; set; }

        [Output]
        public string BinaryPath { get; set; }

        public bool Interactive { get; set; }

        public override bool Execute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.LogWarning("Unable to determine the location of Space Engineers, because we're not running on Windows.");
                return false;
            }
            
            var se = new SpaceEngineers();
            DataPath = se.GetDataPath();
            if (se.TryGetInstallPath("Bin64", out var installPath))
            {
                BinaryPath = installPath;
                Log.LogMessage(MessageImportance.High, $"Successfully determined the binary path of Space Engineers: {BinaryPath}");
                return true;
            }

            if (Interactive)
            { }
            else
                Log.LogWarning("Unable to determine the location of Space Engineers. Do you have the game installed?");

            return false;
        }
    }
}
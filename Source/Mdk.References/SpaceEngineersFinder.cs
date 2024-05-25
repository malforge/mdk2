// Mdk.References
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.IO;
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
        
        public string ProjectPath { get; set; }

        public override bool Execute()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.LogWarning("Unable to determine the location of Space Engineers, because we're not running on Windows.");
                return false;
            }
            
            if (!string.IsNullOrEmpty(ProjectPath))
            {
                var localIniFileName = Path.ChangeExtension(ProjectPath, ".mdk.local.ini");
                if (File.Exists(localIniFileName))
                {
                    Log.LogMessage(MessageImportance.High, $"Found local ini file: {localIniFileName}");
                    if (LoadFromIni(localIniFileName)) return true;
                }
                
                var iniFileName = Path.ChangeExtension(ProjectPath, ".mdk.ini");
                if (File.Exists(iniFileName))
                {
                    Log.LogMessage(MessageImportance.High, $"Found ini file: {iniFileName}");
                    if (LoadFromIni(iniFileName)) return true;
                }
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
        
        bool LoadFromIni(string localIniFileName)
        {
            try
            {
                var content = File.ReadAllText(localIniFileName);
                if (Ini.TryParse(content, out var ini))
                {
                    var section = ini["mdk"];
                    if (section.HasKey("binarypath"))
                    {
                        var path = section["binarypath"].ToString()?.Trim();
                        if (!string.IsNullOrEmpty(path) && !string.Equals(path, "auto", StringComparison.OrdinalIgnoreCase))
                        {
                            BinaryPath = path;
                            Log.LogMessage(MessageImportance.High, $"Binary path of Space Engineers was overridden by ini file: {BinaryPath}");
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.LogWarning($"Failed to read local ini file: {localIniFileName}");
                Log.LogWarningFromException(e);
            }
            
            return false;
        }
    }
}
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
        
        public bool Verbose { get; set; }

        static string GetCustomAutoBinaryPath()
        {
            try
            {
                var settingsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MDK2", "Hub", "settings.json");

                if (!File.Exists(settingsPath))
                    return null;

                var json = File.ReadAllText(settingsPath);
                
                // Simple JSON parsing for just this one value
                var key = "\"CustomAutoBinaryPath\"";
                var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
                if (keyIndex < 0)
                    return null;
                    
                var colonIndex = json.IndexOf(':', keyIndex);
                if (colonIndex < 0)
                    return null;
                    
                var valueStart = json.IndexOf('"', colonIndex + 1);
                if (valueStart < 0)
                    return null;
                    
                var valueEnd = json.IndexOf('"', valueStart + 1);
                if (valueEnd < 0)
                    return null;
                    
                var value = json.Substring(valueStart + 1, valueEnd - valueStart - 1);
                if (string.IsNullOrWhiteSpace(value) || value == "auto")
                    return null;
                    
                return value;
            }
            catch
            {
                // If we can't read settings, just return null (use default)
                return null;
            }
        }

        public override bool Execute()
        {
            if (Verbose)
            {
                Log.LogMessage(MessageImportance.High, "[SpaceEngineersFinder] Starting search...");
                Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] ProjectPath: {ProjectPath ?? "(null)"}");
                Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Interactive: {Interactive}");
            }
            
            if (!string.IsNullOrEmpty(ProjectPath))
            {
                if (Verbose)
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Looking for local ini file...");
                    
                var localIniFileName = IniFileFinder.FindLocalIni(ProjectPath);
                
                if (Verbose)
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] FindLocalIni returned: {localIniFileName ?? "(null)"}");
                    
                if (localIniFileName != null)
                {
                    Log.LogMessage(MessageImportance.High, $"Found local ini file: {localIniFileName}");
                    
                    if (Verbose)
                        Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Attempting to load from ini...");
                        
                    if (LoadFromIni(localIniFileName)) return true;
                }
                
                if (Verbose)
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Looking for main ini file...");
                
                var iniFileName = IniFileFinder.FindMainIni(ProjectPath);
                
                if (Verbose)
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] FindMainIni returned: {iniFileName ?? "(null)"}");
                
                if (iniFileName != null)
                {
                    Log.LogMessage(MessageImportance.High, $"Found ini file: {iniFileName}");
                    
                    if (Verbose)
                        Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Attempting to load from ini...");
                        
                    if (LoadFromIni(iniFileName)) return true;
                }
            }
            else if (Verbose)
            {
                Log.LogMessage(MessageImportance.High, "[SpaceEngineersFinder] ProjectPath is null or empty, skipping ini file search");
            }
            
            if (Verbose)
                Log.LogMessage(MessageImportance.High, "[SpaceEngineersFinder] No ini file found, attempting registry lookup...");
            
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Log.LogWarning("Unable to determine the location of Space Engineers, because we're not running on Windows. If you have a .mdk.local.ini file, you can specify the BinaryPath there.");
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
        
        bool LoadFromIni(string localIniFileName)
        {
            try
            {
                if (Verbose)
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Reading file: {localIniFileName}");
                    
                var content = File.ReadAllText(localIniFileName);
                
                if (Verbose)
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] File content length: {content.Length} chars");
                
                if (Ini.TryParse(content, out var ini))
                {
                    if (Verbose)
                        Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] INI parsed successfully");
                        
                    var section = ini["mdk"];
                    
                    if (Verbose)
                        Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Got section 'mdk'");
                    
                    if (section.HasKey("binarypath"))
                    {
                        var path = section["binarypath"].ToString()?.Trim();
                        
                        if (Verbose)
                            Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] binarypath value: {path ?? "(null)"}");
                        
                        if (!string.IsNullOrEmpty(path) && !string.Equals(path, "auto", StringComparison.OrdinalIgnoreCase))
                        {
                            BinaryPath = path;
                            Log.LogMessage(MessageImportance.High, $"Binary path of Space Engineers was overridden by ini file: {BinaryPath}");
                            return true;
                        }
                        else
                        {
                            // Check global settings for custom auto path
                            var customPath = GetCustomAutoBinaryPath();
                            if (customPath != null)
                            {
                                BinaryPath = customPath;
                                Log.LogMessage(MessageImportance.High, $"Binary path was overridden by global settings: {BinaryPath}");
                                return true;
                            }
                            else if (Verbose)
                            {
                                Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] binarypath is empty or 'auto', skipping");
                            }
                        }
                    }
                    else if (Verbose)
                    {
                        Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Section 'mdk' does not have 'binarypath' key");
                    }
                }
                else if (Verbose)
                {
                    Log.LogMessage(MessageImportance.High, $"[SpaceEngineersFinder] Failed to parse INI file");
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
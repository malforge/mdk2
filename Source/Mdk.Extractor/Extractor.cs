// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.IO;

namespace Mdk.Extractor;

public class Extractor
{
    const string LauncherTypeName = "Mdk.Extractor.Launcher";

    public Extractor(string modWhitelist, string pbWhitelist, string terminal, string seBinPath)
    {
        Current = this;
        ModWhitelist = modWhitelist;
        PbWhitelist = pbWhitelist;
        Terminal = terminal;
        SeBinPath = seBinPath;
    }

    public static Extractor Current { get; private set; }

    public string ModWhitelist { get; }
    public string PbWhitelist { get; }
    public string Terminal { get; }
    public string SeBinPath { get; }

    public void Run()
    {
        AssemblyManager.Init(SeBinPath);

        var path = Path.Combine(Path.GetTempPath(), "TempSEPath");
        var directory = new DirectoryInfo(path);
        if (!directory.Exists)
            directory.Create();

        File.WriteAllText(Path.Combine(path, "SpaceEngineers.cfg"), Resources.SpaceEngineersCfg);

        try
        {
            var launcher = (ILauncher)Activator.CreateInstance(typeof(Program).Assembly.GetType(LauncherTypeName, true));
            launcher.Path = SeBinPath;
            launcher.Launch("-nosplash", "-skipintro", "-appdata", path);
        }
        finally
        {
            Cleanup(directory);
        }
    }

    void Cleanup(DirectoryInfo directory)
    {
        foreach (var subdirectory in directory.GetDirectories())
        {
            Cleanup(subdirectory);
            try
            {
                subdirectory.Delete();
            }
            catch (Exception)
            {
                Console.WriteLine($@"Unable to clean up {subdirectory.FullName}");
            }
        }

        foreach (var file in directory.GetFiles())
        {
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                Console.WriteLine($@"Unable to clean up {file.FullName}");
            }
        }
    }
}
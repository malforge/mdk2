// Mdk.Extractor
// 
// Copyright 2023 Morten A. Lyrstad

using System;
using System.Collections.Generic;
using SpaceEngineers;
using VRage.FileSystem;
using VRage.Plugins;

namespace Mdk.Extractor
{
    // ReSharper disable once UnusedType.Global
    public class Launcher : ILauncher
    {
        public string Path { get; set; }

        public void Launch(string[] args)
        {
            var name = new Uri(typeof(Launcher).Assembly.Location).LocalPath;
            MyFileSystem.ExePath = Path;
            MyFileSystem.RootPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, ".."));

            MyPlugins.RegisterUserAssemblyFiles([name]);
            MyProgram.Main(args);
        }
    }
}
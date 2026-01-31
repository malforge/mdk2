// Mdk.Extractor
// 
// Copyright 2023-2026 The MDK² Authors

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Mdk.Extractor
{
    public class AssemblyManager
    {
        static readonly Dictionary<string, AssemblyName> AssemblyNames = new Dictionary<string, AssemblyName>();

        /// <summary>
        ///     Initializes the mock system. Pass in the path to the Space Engineers Bin64 folder.
        /// </summary>
        public static void Init(params string[] assemblyPaths)
        {
            foreach (var path in assemblyPaths)
            {
                var directory = new DirectoryInfo(path);
                foreach (var dllFileName in directory.EnumerateFiles())
                {
                    switch (dllFileName.Extension.ToUpperInvariant())
                    {
                        case ".EXE":
                        case ".DLL":
                            break;
                        default:
                            continue;
                    }

                    AssemblyName assemblyName;
                    try
                    {
                        assemblyName = AssemblyName.GetAssemblyName(dllFileName.FullName);
                    }
                    catch (BadImageFormatException)
                    {
                        // Not a .NET assembly or wrong platform, ignore
                        continue;
                    }

                    AssemblyNames[assemblyName.FullName] = assemblyName;
                }

                AppDomain.CurrentDomain.AssemblyResolve += OnResolveAssembly;
            }
        }

        /// <summary>
        ///     Gets the game binary path as defined through <see cref="Init" />.
        /// </summary>
        public static string GameBinPath { get; internal set; }

        static Assembly OnResolveAssembly(object sender, ResolveEventArgs args)
        {
            if (AssemblyNames.TryGetValue(args.Name, out var assemblyName))
                return Assembly.Load(assemblyName);
            return null;
        }
    }
}
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Mdk.CommandLine.Shared.Api;
using Microsoft.Build.Locator;

namespace Mdk.CommandLine;

/// <summary>
///     MSBuild registration
/// </summary>
public static class MsBuild
{
    // /// <summary>
    // /// Determines if MSBuild is registered.
    // /// </summary>
    // public static bool IsRegistered { get; private set; }

    /// <summary>
    ///     Installs the MSBuild instance available on the system.
    /// </summary>
    /// <param name="console"></param>
    public static bool Install(IConsole console)
    {
        // if (IsRegistered)
        //     return true;
        //
        // var minVersion = new Version(8, 0);
        // var locator = new Locator();
        // if (!locator.TryFindDotNet(minVersion, out var paths))
        //     return false;
        //
        // foreach (var (version, path) in paths)
        //     console.Trace($"Found .NET SDK: {version} at {path}");
        //
        // var selectedPath = paths.FirstOrDefault();
        // MSBuildLocator.RegisterMSBuildPath(selectedPath.path);
        // IsRegistered = true;
        // // Register(selectedPath.path);

        if (MSBuildLocator.IsRegistered)
            return true;
        
        var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances()
            .Where(x => x.Version.Major >= 8) 
            .OrderByDescending(x => x.Version)
            .ToList();
        foreach (var instance in msbuildInstances)
            console.Trace($"Found MSBuild instance: {instance.Name} {instance.Version}");
        var selectedInstance = msbuildInstances.FirstOrDefault();
        if (selectedInstance == null) return false;
        MSBuildLocator.RegisterInstance(selectedInstance);
        return true;
    }

    // static void Register(string dotNetSdkPath)
    // {
    //     Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", Path.Combine(dotNetSdkPath, "MSBuild.dll"));
    //     Environment.SetEnvironmentVariable("MSBuildExtensionsPath", dotNetSdkPath);
    //     Environment.SetEnvironmentVariable("MSBuildSDKsPath", Path.Combine(dotNetSdkPath, "Sdks"));
    // }

    class Locator
    {
        public bool TryFindDotNet(Version minVersion, out ImmutableArray<(Version version, string path)> paths)
        {
            var result = GetFromDotNetRoot(minVersion);
            if (result.Length > 0)
            {
                paths = result;
                return true;
            }

            result = GetFromDotNet(minVersion);
            if (result.Length > 0)
            {
                paths = result;
                return true;
            }

            result = GetFromStandards(minVersion);
            paths = result;
            return result.Length > 0;
        }

        ImmutableArray<(Version version, string path)> GetFromDotNetRoot(Version minVersion)
        {
            var dotnetRoot = Environment.GetEnvironmentVariable("DOTNET_ROOT");
            if (string.IsNullOrEmpty(dotnetRoot))
                return ImmutableArray<(Version version, string path)>.Empty;

            var sdkPath = Path.Combine(dotnetRoot, "sdk");
            if (!Directory.Exists(sdkPath))
                return ImmutableArray<(Version version, string path)>.Empty;

            var result = ImmutableArray.CreateBuilder<(Version version, string path)>();
            foreach (var subpath in Directory.GetDirectories(sdkPath))
            {
                var versionStr = Path.GetFileName(subpath);
                if (Version.TryParse(versionStr, out var version) && version >= minVersion)
                    result.Add((version, subpath));
            }

            return result.OrderByDescending(x => x.version)
                .GroupBy(x => x.version.Major)
                .Select(x => x.First())
                .ToImmutableArray();
        }

        ImmutableArray<(Version version, string path)> GetFromDotNet(Version minVersion)
        {
            var dotnet = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "dotnet.exe"
                : "dotnet";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = dotnet,
                    Arguments = "--list-sdks",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var result = ImmutableArray.CreateBuilder<(Version version, string path)>();
            foreach (var line in output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
            {
                var lBraceIndex = line.IndexOf('[');
                var rBraceIndex = line.LastIndexOf(']');
                if (lBraceIndex == -1 || rBraceIndex == -1)
                    continue;

                var sdkPath = line.Substring(lBraceIndex + 1, rBraceIndex - lBraceIndex - 1);
                var sdkVersionStr = line[..(lBraceIndex - 1)];
                if (Directory.Exists(sdkPath) && Version.TryParse(sdkVersionStr, out var sdkVersion) && sdkVersion >= minVersion)
                    result.Add((sdkVersion, sdkPath));
            }

            return result.OrderByDescending(x => x.version)
                .GroupBy(x => x.version.Major)
                .Select(x => x.First())
                .ToImmutableArray();
        }

        ImmutableArray<(Version version, string path)> GetFromStandards(Version minVersion)
        {
            string[] commonPaths = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? [@"C:\Program Files\dotnet\sdk"]
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                    ? ["/usr/share/dotnet/sdk", "/usr/local/share/dotnet/sdk"]
                    : ["/usr/local/share/dotnet/sdk", "/usr/share/dotnet/sdk"]; // macOS

            var result = ImmutableArray.CreateBuilder<(Version version, string path)>();
            foreach (var path in commonPaths)
            {
                if (Directory.Exists(path))
                {
                    Console.WriteLine($"Using SDKs from common path: {path}");
                    foreach (var subpath in Directory.GetDirectories(path))
                    {
                        var versionStr = Path.GetFileName(subpath);
                        if (Version.TryParse(versionStr, out var version) && version >= minVersion)
                            result.Add((version, subpath));
                    }
                }
            }

            return result.OrderByDescending(x => x.version)
                .GroupBy(x => x.version.Major)
                .Select(x => x.First())
                .ToImmutableArray();
        }
    }
}
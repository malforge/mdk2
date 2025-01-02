using System.Linq;
using Mdk.CommandLine.Shared.Api;
using Microsoft.Build.Locator;

namespace Mdk.CommandLine;

/// <summary>
/// MSBuild registration
/// </summary>
public static class MsBuild
{
    /// <summary>
    /// Installs the MSBuild instance available on the system.
    /// </summary>
    /// <param name="console"></param>
    public static bool Install(IConsole console)
    {
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
}
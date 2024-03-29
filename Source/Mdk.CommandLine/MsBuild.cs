using System.Linq;
using Mdk.CommandLine.SharedApi;
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
    public static void Install(IConsole console)
    {
        if (MSBuildLocator.IsRegistered)
            return;
        
        var msbuildInstances = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(x => x.Version).ToArray();
        foreach (var instance in msbuildInstances)
            console.Trace($"Found MSBuild instance: {instance.Name} {instance.Version}");
        MSBuildLocator.RegisterInstance(msbuildInstances.First());
    }
}
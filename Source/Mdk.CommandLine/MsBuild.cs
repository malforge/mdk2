using System.Linq;
using Mdk.CommandLine.SharedApi;
using Microsoft.Build.Locator;

namespace Mdk.CommandLine;

public static class MsBuild
{
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
using System.Collections.Immutable;
using Malforge.Mdk2.Setup.Foundation;

namespace Malforge.Mdk2.Setup;

public static class Installer
{
    public static readonly ImmutableArray<InstallerStep> Steps =
    [
        new DotNetInstallerStep(),
        new MdkInstallerStep(),
        new MdkTemplatesInstallerStep()
    ];
}
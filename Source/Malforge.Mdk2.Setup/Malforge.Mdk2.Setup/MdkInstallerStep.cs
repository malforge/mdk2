using System.Threading;
using System.Threading.Tasks;
using Malforge.Mdk2.Setup.Foundation;
using Semver;

namespace Malforge.Mdk2.Setup;

public class MdkInstallerStep() : InstallerStep("Mdk²")
{
    public override async Task RunAsync(CancellationToken cancellationToken = default)
    {
        CurrentOperation = "Checking Mdk² installation...";
        // SemVersion? pbAnalyzersVersion = null;
        // SemVersion? pbPackagerVersion = null;
        // SemVersion? modAnalyzersVersion = null;
        // SemVersion? modPackagerVersion = null;
        // SemVersion? referencesVersion = null;
        // SemVersion? scriptTemplatesVersion = null;
        //
        // await Task.WhenAll(
        //     Nuget.GetPackageVersionAsync(
        //         "Mal.Mdk2.PbAnalyzers",
        //         cancellationToken: cancellationToken
        //     ).ContinueWith(t =>
        //         {
        //             if (t.IsCompletedSuccessfully)
        //                 pbAnalyzersVersion = t.Result;
        //         },
        //         cancellationToken),
        //     Nuget.GetPackageVersionAsync(
        //         "Mal.Mdk2.PbPackager",
        //         cancellationToken: cancellationToken
        //     ).ContinueWith(t =>
        //         {
        //             if (t.IsCompletedSuccessfully)
        //                 pbPackagerVersion = t.Result;
        //         },
        //         cancellationToken),
        //     Nuget.GetPackageVersionAsync(
        //         "Mal.Mdk2.ModAnalyzers",
        //         cancellationToken: cancellationToken
        //     ).ContinueWith(t =>
        //         {
        //             if (t.IsCompletedSuccessfully)
        //                 modAnalyzersVersion = t.Result;
        //         },
        //         cancellationToken),
        //     Nuget.GetPackageVersionAsync(
        //         "Mal.Mdk2.ModPackager",
        //         cancellationToken: cancellationToken
        //     ).ContinueWith(t =>
        //         {
        //             if (t.IsCompletedSuccessfully)
        //                 modPackagerVersion = t.Result;
        //         },
        //         cancellationToken),
        //     Nuget.GetPackageVersionAsync(
        //         "Mal.Mdk2.References",
        //         cancellationToken: cancellationToken
        //     ).ContinueWith(t =>
        //         {
        //             if (t.IsCompletedSuccessfully)
        //                 referencesVersion = t.Result;
        //         },
        //         cancellationToken),
        //     Nuget.GetPackageVersionAsync(
        //         "Mal.Mdk2.ScriptTemplates",
        //         cancellationToken: cancellationToken
        //     ).ContinueWith(t =>
        //         {
        //             if (t.IsCompletedSuccessfully)
        //                 scriptTemplatesVersion = t.Result;
        //         },
        //         cancellationToken)
        // ).ConfigureAwait(false);

        CurrentOperation = "Success.";
    }
}
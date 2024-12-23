using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Mod.Restore;

/// <summary>
///     Runs checks and restores MDK projects if necessary.
/// </summary>
public class ScriptRestorer : ProjectJob
{
    /// <summary>
    ///     Run the restoration job.
    /// </summary>
    /// <param name="parameters"></param>
    /// <param name="project"></param>
    /// <param name="console"></param>
    /// <param name="httpClient"></param>
    /// <param name="interaction"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task RestoreAsync(Parameters parameters, MdkProject project, IConsole console, IHttpClient httpClient, IInteraction interaction)
    {
        parameters.DumpTrace(console);

        var projectFileName = parameters.RestoreVerb.ProjectFile ?? throw new InvalidOperationException("Project file not specified.");
        XDocument document;
        using (var reader = new StreamReader(projectFileName))
            document = await XDocument.LoadAsync(reader, LoadOptions.None, CancellationToken.None);

        var tasks = new[]
        {
            NugetVersionCheck.CheckAsync(document, projectFileName, "Mal.Mdk2.PbPackager", console, httpClient, interaction),
            NugetVersionCheck.CheckAsync(document, projectFileName, "Mal.Mdk2.PbAnalyzers", console, httpClient, interaction),
            NugetVersionCheck.CheckAsync(document, projectFileName, "Mal.Mdk2.References", console, httpClient, interaction),
            BadIgnoresBugFix.CheckAsync(projectFileName, console)
        };
        
        await Task.WhenAll(tasks);
    }
}
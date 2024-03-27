using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.IngameScript.Pack.Api;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.IngameScript.Pack.DefaultProcessors;

/// <summary>
///     The default script producer.
/// </summary>
public class Producer : IScriptProducer
{
    /// <inheritdoc />
    public async Task ProduceAsync(DirectoryInfo outputDirectory, StringBuilder script, TextDocument? readmeDocument, TextDocument? thumbnailDocument, IPackContext context)
    {
        context.Console.Trace("Writing the combined syntax tree to a file");
        var outputPath = Path.Combine(outputDirectory.FullName, "script.cs");
        var buffer = new StringBuilder();
        if (readmeDocument != null)
        {
            var readmeText = await readmeDocument.GetTextAsync();
            buffer.Append("// " + string.Join("\n// ", readmeText.Lines.Select(l => l.ToString()))).Append('\n');
        }
        buffer.Append(script.ToString().Replace(Environment.NewLine, "\n"));
        await File.WriteAllTextAsync(outputPath, buffer.ToString());
        context.Console.Trace($"The combined syntax tree was written to {outputPath}");
        if (thumbnailDocument != null)
        {
            var thumbnailPath = Path.Combine(outputDirectory.FullName, "thumb.png");
            File.Copy(thumbnailDocument.FilePath!, thumbnailPath, true);
            context.Console.Trace($"The thumbnail was written to {thumbnailPath}");
        }
    }
}
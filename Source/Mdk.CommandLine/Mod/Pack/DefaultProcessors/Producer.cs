using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mdk.CommandLine.Mod.Pack.Api;
using Mdk.CommandLine.SharedApi;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Mod.Pack.DefaultProcessors;

/// <summary>
///     The default script producer.
/// </summary>
public class Producer : IModProducer
{
    /// <inheritdoc />
    public async Task<ImmutableArray<ProducedFile>> ProduceAsync(DirectoryInfo outputDirectory, StringBuilder script, TextDocument? readmeDocument, TextDocument? thumbnailDocument, IPackContext context)
    {
        if (context.Parameters.PackVerb.DryRun)
        {
            context.Console.Trace("Dry run mode is enabled, so the script will not be written to disk");
        }
        else
        {
            context.Console.Trace("Writing the combined syntax tree to a file");
        }
        var fileBuilder = ImmutableArray.CreateBuilder<ProducedFile>();
        
        var outputPath = Path.Combine(outputDirectory.FullName, "script.cs");
        var buffer = new StringBuilder();
        if (readmeDocument != null)
        {
            var readmeText = await readmeDocument.GetTextAsync();
            buffer.Append("// " + string.Join("\n// ", readmeText.Lines.Select(l => l.ToString()))).Append('\n');
        }
        buffer.Append(script.ToString().Replace(Environment.NewLine, "\n"));
        fileBuilder.Add(new ProducedFile("script.cs", outputPath, buffer.ToString()));
        if (!context.Parameters.PackVerb.DryRun)
        {
            await File.WriteAllTextAsync(outputPath, buffer.ToString());
            context.Console.Trace($"The combined syntax tree was written to {outputPath}");
        }
        else
        {
            context.Console.Trace($"The combined syntax tree would have been written to {outputPath}");
        }
        if (thumbnailDocument != null)
        {
            var thumbnailPath = Path.Combine(outputDirectory.FullName, "thumb.png");
            fileBuilder.Add(new ProducedFile("thumb.png", thumbnailPath, null));
            if (!context.Parameters.PackVerb.DryRun)
            {
                File.Copy(thumbnailDocument.FilePath!, thumbnailPath, true);
                context.Console.Trace($"The thumbnail was written to {thumbnailPath}");
            }
            else
            {
                context.Console.Trace($"The thumbnail would have been written to {thumbnailPath}");
            }
        }
        
        return fileBuilder.ToImmutable();
    }
}
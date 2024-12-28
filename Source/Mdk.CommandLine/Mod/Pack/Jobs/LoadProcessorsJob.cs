using System.Threading.Tasks;

namespace Mdk.CommandLine.Mod.Pack.Jobs;

internal class LoadProcessorsJob : ModJob
{
    public override Task<ModPackContext> ExecuteAsync(ModPackContext context)
    {
        context.Console.Trace("Loading processors");
        var manager = ModProcessingManager.Create().Build();
        var processors = manager.Processors;
        return Task.FromResult(context.WithProcessors(processors));
    }
}
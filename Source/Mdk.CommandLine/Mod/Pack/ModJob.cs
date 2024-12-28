using System.Threading.Tasks;

namespace Mdk.CommandLine.Mod.Pack;

/// <summary>
///     This is the base class for all jobs that can be executed by the packer.
/// </summary>
internal abstract class ModJob
{
    /// <summary>
    ///     Executes the job.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public abstract Task ExecuteAsync(IModPackContext context);
}
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Mdk.CommandLine.Shared.Api;

public interface IProjectProcessor
{
    Task<Project> ProcessAsync(Project project, IPackContext context, CancellationToken cancellationToken = default);
}

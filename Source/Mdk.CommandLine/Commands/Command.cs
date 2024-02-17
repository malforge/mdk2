using System.Collections.Generic;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Commands;

public abstract class Command
{
    public abstract Task ExecuteAsync(List<string> arguments, IConsole console);
    public abstract void Help(IConsole console);
}
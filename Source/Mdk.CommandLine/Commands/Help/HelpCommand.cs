using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine.Commands.Help;

public class HelpCommand : Command
{
    public override async Task ExecuteAsync(List<string> arguments, IConsole console)
    {
        Title(console);

        var commands = Program.Commands;
        if (arguments.Count == 0)
        {
            console.Print("Available commands:");
            foreach (var command in commands.Keys) console.Print($"  {command}");
        }
        else
        {
            var commandName = arguments.Dequeue();
            if (commands.TryGetValue(commandName, out var command))
                command.Help(console);
            else
                console.Print($"Unknown command: {commandName}");
        }

        await Task.Yield();
    }

    public override void Help(IConsole console)
    {
        Title(console);
        console.Print("Usage: help [command]")
            .Print("  If no command is specified, lists all available commands.")
            .Print("  If a command is specified, shows help for that command.");
    }

    static void Title(IConsole console)
    {
        string header;
        var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (version != null)
        {
            var lastPlusIndex = version.LastIndexOf('+');
            if (lastPlusIndex >= 0) version = version[..lastPlusIndex];
            header = $"MDK v{version}";
        }
        else
            header = "MDK Development Version";

        console.Print(header)
            .Print(new string('=', header.Length))
            .Print();
    }
}
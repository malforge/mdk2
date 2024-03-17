using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

public class Interaction : IInteraction
{
    readonly IConsole _console;
    readonly string? _notifyPath;

    public Interaction(IConsole console, bool interactive)
    {
        _console = console;
        if (!interactive || !OperatingSystem.IsWindows())
            return;
        
        if (File.Exists("mdknotify-win.exe"))
            _notifyPath = "mdknotify-win.exe";
        else
        {
            var path = Environment.GetEnvironmentVariable("Path");
            if (path is not null)
            {
                foreach (var dir in path.Split(';'))
                {
                    var file = Path.Combine(dir, "mdknotify-win.exe");
                    if (File.Exists(file))
                    {
                        _notifyPath = file;
                        break;
                    }
                }
            }
        }
    }

    static string Escape(string value)
    {
        var content = value.Replace("&", "&amp;");
        if (content.Length == 0)
            return "\"\"";
        if (content.Contains(' '))
            return $"\"{content}\"";
        return content;
    }

    public void Notify(InteractionType type, string? message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
        {
            message = string.Empty;            
        }
        else
        {
            message = string.Format(message, args);
        }
        switch (type)
        {
            case InteractionType.Normal:
                _console.Print(message);
                break;
            case InteractionType.Script:
                if (message.Length == 0)
                    _console.Print($"The script {message} has been executed.");
                else
                    _console.Print(message);
                break;
            case InteractionType.NugetPackageVersionAvailable:
                if (message.Length == 0)
                    _console.Print($"The {message} nuget package has a new version available: {args[0]} -> {args[1]}");
                else
                    _console.Print(message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
        Run($"{type.ToString().ToLowerInvariant()} \"{Escape(message)}\" {string.Join(" ", args.Select(a => Escape(a?.ToString() ?? string.Empty)))}");
    }

    void Run(string arguments)
    {
        if (_notifyPath is null)
            return;
        var notify = new Process
        {
            StartInfo =
            {
                FileName = _notifyPath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        try
        {
            notify.Start();
        }
        catch
        {
            _console.Print("Failed to run mdknotify-win.exe");
        }
    }
}
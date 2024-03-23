using System;
using System.Diagnostics;
using System.IO;
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

    public void Custom(string message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            return;
        message = string.Format(message, args);
        var arguments = $"custom {Escape(message)}";
        _console.Trace($"Running: mdknotify-win.exe {arguments}");  
        Run(arguments);
    }

    public void Script(string scriptName, string folder, string? message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            message = $"Your script \"{scriptName}\" has been successfully deployed.";
        else
            message = string.Format(message, args);
        var arguments = $"script {Escape(scriptName)} {Escape(folder)} {Escape(message)}";
        _console.Trace($"Running: mdknotify-win.exe {arguments}");
        Run(arguments);
    }

    public void Nuget(string packageName, string currentVersion, string newVersion, string? message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            message = $"The {message} nuget package has a new version available: {currentVersion} -> {newVersion}";
        else
            message = string.Format(message, args);
        var arguments = $"nuget {Escape(packageName)} {Escape(currentVersion)} {Escape(newVersion)} {Escape(message)}";
        _console.Trace($"Running: mdknotify-win.exe {arguments}");
        Run(arguments);
    }

    static string Escape(string value)
    {
        var content = value.Replace("\"", "&quot;");
        if (content.Length == 0)
            return "\"\"";
        if (content.Contains(' '))
            return $"\"{content}\"";
        return content;
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
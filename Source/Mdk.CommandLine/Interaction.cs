using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

public class Interaction : IInteraction
{
    readonly IConsole _console;
    readonly string? _notifyPath;

    static IEnumerable<string> PotentialLocations()
    {
        yield return Path.GetFullPath("mdknotify-win.exe");
        yield return Path.Combine(AppContext.BaseDirectory, "mdknotify-win.exe");
        var path = Environment.GetEnvironmentVariable("Path");
        if (path is not null)
        {
            foreach (var dir in path.Split(';'))
                yield return Path.Combine(dir, "mdknotify-win.exe");
        }
    }
    
    public Interaction(IConsole console, bool interactive)
    {
        _console = console;
        if (!interactive)
        {
            console.Trace("Interaction disabled.");
        }
        if (!OperatingSystem.IsWindows())
        {
            console.Trace("Interaction is only supported on Windows.");
            return;
        }

        _notifyPath = PotentialLocations().FirstOrDefault(File.Exists);
        if (_notifyPath is null)
        {
            console.Trace("mdknotify-win.exe not found.");
        }
        else
            console.Trace($"mdknotify-win.exe found at: {_notifyPath}");
    }

    public void Custom(string message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            return;
        message = string.Format(message, args);
        _console.Print(message);
        if (_notifyPath is null)
            return;
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
        _console.Print(message);
        if (_notifyPath is null)
            return;
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
        _console.Print(message);
        if (_notifyPath is null)
            return;
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
        catch (Exception e)
        {
            _console.Print("Failed to run mdknotify-win.exe");
            _console.Trace(e.ToString());
        }
    }
}
/*
 *             try
            {
                var notify = new Process
                {
                    StartInfo =
                    {
                        FileName = notifierExe,
                        Arguments = $"script \"{project.Name}\" \"{outputDirectory.FullName}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                notify.Start();
            }
            catch (Exception e)
            {
                console.Print("Failed to run mdknotify-win.");
                console.Print(e.ToString());
                return false;
            }

 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine;

public class Interaction : IInteraction
{
    readonly IConsole _console;
    readonly string? _hubPath;
    
    public Interaction(IConsole console, bool interactive)
    {
        _console = console;
        if (!interactive)
        {
            console.Trace("Interaction disabled.");
            return;
        }

        _hubPath = HubLocator.FindHub(console);
        if (_hubPath is null)
        {
            console.Trace("Hub not found - showing notification to user.");
            HubLocator.ShowHubNotFoundMessage();
        }
    }

    public void Custom(string message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            return;
        message = string.Format(message, args);
        _console.Print(message);
        if (_hubPath is null)
            return;
        var arguments = $"custom {Escape(message)}";
        _console.Trace($"Running Hub: {arguments}");  
        Run(arguments);
    }

    public void Script(string scriptName, string projectPath, string? message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            message = $"Your script \"{scriptName}\" has been successfully deployed.";
        else
            message = string.Format(message, args);
        _console.Print(message);
        if (_hubPath is null)
            return;
        var arguments = $"script {Escape(scriptName)} {Escape(projectPath)} {Escape(message)}";
        _console.Trace($"Running Hub: {arguments}");
        Run(arguments);
    }

    public void Nuget(string packageName, string currentVersion, string newVersion, string? message, params object?[] args)
    {
        if (string.IsNullOrEmpty(message))
            message = $"The {packageName} nuget package has a new version available: {currentVersion} -> {newVersion}";
        else
            message = string.Format(message, args);
        _console.Print(message);
        if (_hubPath is null)
            return;
        var arguments = $"nuget {Escape(packageName)} {Escape(currentVersion)} {Escape(newVersion)} {Escape(message)}";
        _console.Trace($"Running Hub: {arguments}");
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
        if (_hubPath is null)
            return;
        var hub = new Process
        {
            StartInfo =
            {
                FileName = _hubPath,
                Arguments = arguments,
                UseShellExecute = true
            }
        };
        try
        {
            hub.Start();
        }
        catch (Exception e)
        {
            _console.Print("Failed to run Hub");
            _console.Trace(e.ToString());
        }
    }
}
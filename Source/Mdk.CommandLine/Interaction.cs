using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Mdk.CommandLine.CommandLine;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine;

public class Interaction : IInteraction
{
    readonly IConsole _console;
    readonly string? _hubPath;
    readonly InteractiveMode? _interactiveMode;
    
    public Interaction(IConsole console, InteractiveMode? interactiveMode)
    {
        _console = console;
        _interactiveMode = interactiveMode;
        
        // If interactive mode is explicitly set to DoNothing, disable interaction
        if (interactiveMode == InteractiveMode.DoNothing)
        {
            console.Trace("Interaction disabled by command-line.");
            return;
        }
        
        // If set to ShowNotification or OpenHub (or null - use default), enable interaction
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
        var arguments = BuildArguments("custom", Escape(message));
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
        var arguments = BuildArguments("script", Escape(scriptName), Escape(projectPath), Escape(message));
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
        var arguments = BuildArguments("nuget", Escape(packageName), Escape(currentVersion), Escape(newVersion), Escape(message));
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

    string BuildArguments(string command, params string[] args)
    {
        var baseArgs = $"{command} {string.Join(" ", args)}";
        
        // If interactive mode is set, append it as a command-line argument for Hub
        if (_interactiveMode.HasValue)
            return $"{baseArgs} --interactive {_interactiveMode.Value}";
        
        return baseArgs;
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
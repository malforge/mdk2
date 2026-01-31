using System;
using System.Diagnostics;
using System.IO;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine;

/// <summary>
/// Locates the MDK Hub executable for interactive mode.
/// </summary>
public static class HubLocator
{
    /// <summary>
    /// Attempts to find the Hub executable path.
    /// </summary>
    /// <param name="console">Console for logging.</param>
    /// <returns>Hub executable path, or null if not found.</returns>
    public static string? FindHub(IConsole console)
    {
        try
        {
            var appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var pathFile = Path.Combine(appDataFolder, "MDK2", "Hub", "hub.path");
            
            if (!File.Exists(pathFile))
            {
                console.Trace($"Hub path file not found: {pathFile}");
                return null;
            }

            var hubPath = File.ReadAllText(pathFile).Trim();
            if (string.IsNullOrEmpty(hubPath))
            {
                console.Trace("Hub path file is empty");
                return null;
            }

            if (!File.Exists(hubPath))
            {
                console.Trace($"Hub executable not found at: {hubPath}");
                return null;
            }

            console.Trace($"Hub found at: {hubPath}");
            return hubPath;
        }
        catch (Exception ex)
        {
            console.Trace($"Error locating Hub: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Shows a platform-specific message when Hub is not found.
    /// </summary>
    public static void ShowHubNotFoundMessage()
    {
        try
        {
            var htmlPath = Path.Combine(Path.GetTempPath(), "mdk-hub-not-found.html");
            var html = GenerateHelpHtml();
            File.WriteAllText(htmlPath, html);
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = htmlPath,
                    UseShellExecute = true
                }
            };
            process.Start();
        }
        catch
        {
            // Fallback to console
            Console.WriteLine("MDK Hub Not Found");
            Console.WriteLine();
            Console.WriteLine("Interactive mode requires the MDK Hub to be installed.");
            Console.WriteLine("Please visit the MDK website to download the Hub,");
            Console.WriteLine("or add 'interactive=off' to your mdk.local.ini file to disable interactive mode.");
        }
    }

    static string GenerateHelpHtml()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>MDK Hub Not Found</title>
    <style>
        * {
            box-sizing: border-box;
        }
        body {
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            margin: 0;
            padding: 2em;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #0a0a0a 0%, #1a1a2e 50%, #16213e 100%);
            color: #e0e0e0;
        }
        .container {
            max-width: 700px;
            background-color: rgba(26, 26, 46, 0.8);
            border-radius: 10px;
            padding: 2.5em;
            border: 1px solid rgba(255, 255, 255, 0.1);
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.3);
        }
        h1 {
            color: #4da6ff;
            margin-top: 0;
            font-size: 2em;
            margin-bottom: 0.5em;
        }
        p {
            font-size: 1.1em;
            line-height: 1.6;
            color: #d0d0d0;
        }
        .info {
            background-color: rgba(77, 166, 255, 0.1);
            border-left: 4px solid #4da6ff;
            padding: 1em;
            margin: 1.5em 0;
            border-radius: 4px;
        }
        .solutions {
            margin: 2em 0;
        }
        .solution {
            background-color: rgba(0, 0, 0, 0.2);
            padding: 1.5em;
            margin: 1em 0;
            border-radius: 8px;
            border: 1px solid rgba(255, 255, 255, 0.05);
        }
        .solution h3 {
            color: #80b3ff;
            margin-top: 0;
            margin-bottom: 0.5em;
            font-size: 1.3em;
        }
        .solution p {
            margin: 0.5em 0;
            font-size: 1em;
        }
        code {
            background-color: rgba(0, 0, 0, 0.4);
            padding: 0.3em 0.6em;
            border-radius: 4px;
            font-family: 'Consolas', 'Monaco', monospace;
            color: #a8d8ff;
            font-size: 0.95em;
        }
        a.button {
            display: inline-block;
            padding: 0.9em 1.8em;
            font-size: 1.05em;
            color: #fff;
            background-color: #007bff;
            text-decoration: none;
            border-radius: 5px;
            transition: all 0.3s;
            font-weight: 500;
            margin-top: 0.5em;
        }
        a.button:hover {
            background-color: #0056b3;
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(0, 123, 255, 0.4);
        }
        .footer {
            margin-top: 2em;
            padding-top: 1.5em;
            border-top: 1px solid rgba(255, 255, 255, 0.1);
            font-size: 0.95em;
            color: #9db3cc;
        }
        .footer a {
            color: #4da6ff;
            text-decoration: none;
        }
        .footer a:hover {
            text-decoration: underline;
        }
    </style>
</head>
<body>
    <div class="container">
        <h1>MDK Hub Not Found</h1>
        
        <div class="info">
            <p>Interactive mode requires the <strong>MDK Hub</strong> to be installed, but it couldn't be found on your system.</p>
        </div>

        <div class="solutions">
            <div class="solution">
                <h3>Option 1: Install the MDK Hub</h3>
                <p>The Hub provides a visual interface for managing your projects, updates, and notifications.</p>
                <a href="https://github.com/malforge/mdk2/releases" class="button">Download MDK Hub</a>
            </div>
            
            <div class="solution">
                <h3>Option 2: Disable Interactive Mode</h3>
                <p>If you prefer working without the Hub, you can disable interactive mode by adding this line to your <code>mdk.local.ini</code> file:</p>
                <p><code>interactive=off</code></p>
            </div>
        </div>
        
        <div class="footer">
            <p>For more information, visit the <a href="https://malforge.github.io/spaceengineers/mdk2/">MDK2 documentation</a>.</p>
        </div>
    </div>
</body>
</html>
""";
    }

    static bool TryRunCommand(string command, string arguments)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(5000); // Don't wait forever
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

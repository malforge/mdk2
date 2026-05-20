using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Mdk.CommandLine.Shared.Api;

namespace Mdk.CommandLine;

/// <summary>
/// Captures the outcome of a Hub-discovery attempt — both the resolved path (if any) and
/// the diagnostic state describing which check failed. Surfaced to the user in the
/// "Hub Not Found" page so they can self-diagnose stale or misconfigured installations.
/// </summary>
/// <param name="Path">Resolved Hub executable path, or null if discovery failed.</param>
/// <param name="PathFile">The hub.path marker file location that was inspected.</param>
/// <param name="PathFileExists">Whether <paramref name="PathFile"/> exists on disk.</param>
/// <param name="PathFileContents">The trimmed contents of the marker file, if it was readable.</param>
/// <param name="TargetExists">Whether the path read from the marker file points to an existing file.</param>
/// <param name="Error">Exception message captured during lookup, or null if no exception was thrown.</param>
public sealed record HubLocation(
    string? Path,
    string PathFile,
    bool PathFileExists,
    string? PathFileContents,
    bool TargetExists,
    string? Error)
{
    /// <summary>True if Hub was found.</summary>
    public bool Found => Path is not null;
}

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
    public static string? FindHub(IConsole console) => LocateHub(console).Path;

    /// <summary>
    /// Performs Hub discovery and returns the full diagnostic state regardless of outcome.
    /// </summary>
    /// <param name="console">Console for logging.</param>
    /// <param name="pathFileOverride">Optional override for the hub.path marker file location (used by tests).</param>
    public static HubLocation LocateHub(IConsole console, string? pathFileOverride = null)
    {
        var pathFile = pathFileOverride ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MDK2",
            "hub.path");

        try
        {
            if (!File.Exists(pathFile))
            {
                console.Trace($"Hub path file not found: {pathFile}");
                return new HubLocation(null, pathFile, false, null, false, null);
            }

            var hubPath = File.ReadAllText(pathFile).Trim();
            if (string.IsNullOrEmpty(hubPath))
            {
                console.Trace("Hub path file is empty");
                return new HubLocation(null, pathFile, true, hubPath, false, null);
            }

            if (!File.Exists(hubPath))
            {
                console.Trace($"Hub executable not found at: {hubPath}");
                return new HubLocation(null, pathFile, true, hubPath, false, null);
            }

            console.Trace($"Hub found at: {hubPath}");
            return new HubLocation(hubPath, pathFile, true, hubPath, true, null);
        }
        catch (Exception ex)
        {
            console.Trace($"Error locating Hub: {ex.Message}");
            return new HubLocation(null, pathFile, File.Exists(pathFile), null, false, ex.Message);
        }
    }

    /// <summary>
    /// Shows a platform-specific message when Hub is not found.
    /// </summary>
    public static void ShowHubNotFoundMessage(HubLocation? location = null)
    {
        try
        {
            var htmlPath = Path.Combine(Path.GetTempPath(), "mdk-hub-not-found.html");
            var html = GenerateHelpHtml(location);
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

    /// <summary>
    /// Generates the HTML page shown to the user when the Hub isn't found.
    /// Exposed for testing the diagnostic surface.
    /// </summary>
    public static string GenerateHelpHtml(HubLocation? location)
    {
        var diagnostics = RenderDiagnostics(location);
        return $$"""
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
        .diagnostics {
            background-color: rgba(0, 0, 0, 0.3);
            border: 1px solid rgba(255, 255, 255, 0.08);
            padding: 1.2em 1.5em;
            margin: 1.5em 0;
            border-radius: 6px;
            font-size: 0.95em;
        }
        .diagnostics h3 {
            color: #9db3cc;
            margin: 0 0 0.6em 0;
            font-size: 1.05em;
        }
        .diagnostics dl {
            margin: 0;
            display: grid;
            grid-template-columns: max-content 1fr;
            gap: 0.35em 1em;
        }
        .diagnostics dt {
            color: #9db3cc;
        }
        .diagnostics dd {
            margin: 0;
            color: #d0d0d0;
            word-break: break-all;
        }
        .diagnostics .ok { color: #7fd47f; }
        .diagnostics .bad { color: #ff8a8a; }
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
{{diagnostics}}
        <div class="solutions">
            <div class="solution">
                <h3>Option 1: Install the MDK Hub</h3>
                <p>The Hub provides a visual interface for managing your projects, updates, and notifications.</p>
                <a href="https://github.com/malforge/mdk2/releases" class="button">Download MDK Hub</a>
            </div>

            <div class="solution">
                <h3>Option 2: Disable Interactive Mode</h3>
                <p>If you prefer working without the Hub, you can disable interactive mode by adding this line to your <code>mdk.local.ini</code> file:</p>
                <p><code>interactive=DoNothing</code></p>
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

    static string RenderDiagnostics(HubLocation? location)
    {
        if (location is null)
            return string.Empty;

        var pathFileStatus = location.PathFileExists
            ? "<span class=\"ok\">exists</span>"
            : "<span class=\"bad\">missing</span>";

        string contentsRow;
        if (!location.PathFileExists)
            contentsRow = string.Empty;
        else if (string.IsNullOrEmpty(location.PathFileContents))
            contentsRow = "            <dt>Contents</dt>\n            <dd><span class=\"bad\">(empty)</span></dd>\n";
        else
            contentsRow = $"            <dt>Contents</dt>\n            <dd><code>{HtmlEncode(location.PathFileContents)}</code></dd>\n";

        string targetRow;
        if (!location.PathFileExists || string.IsNullOrEmpty(location.PathFileContents))
            targetRow = string.Empty;
        else
        {
            var targetStatus = location.TargetExists
                ? "<span class=\"ok\">exists</span>"
                : "<span class=\"bad\">missing</span>";
            targetRow = $"            <dt>Hub executable</dt>\n            <dd>{targetStatus}</dd>\n";
        }

        var errorRow = string.IsNullOrEmpty(location.Error)
            ? string.Empty
            : $"            <dt>Error</dt>\n            <dd><span class=\"bad\">{HtmlEncode(location.Error)}</span></dd>\n";

        return $"""
        <div class="diagnostics">
            <h3>Diagnostics</h3>
            <dl>
                <dt>Marker file</dt>
                <dd><code>{HtmlEncode(location.PathFile)}</code></dd>
                <dt>Status</dt>
                <dd>{pathFileStatus}</dd>
{contentsRow}{targetRow}{errorRow}            </dl>
        </div>

""";
    }

    static string HtmlEncode(string value) => WebUtility.HtmlEncode(value);
}

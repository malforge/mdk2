using System.Diagnostics;
using System.Runtime.InteropServices;
using CheckDotNet;

Version noVersion = new(0, 0);
Version requiredVersion = new(10, 0);

var interactive = args.Length > 0 && args[0] == "-interactive";

try
{
    var sdkVersion = getInstalledSdkVersion();
    if (sdkVersion < requiredVersion)
    {
        var message = $$"""
                        .NET SDK version {{requiredVersion}} or higher is required to run MDK.

                        It looks like the required version isn't installed on your system. You can download the latest .NET SDK from the official Microsoft website:
                        https://dotnet.microsoft.com/download/dotnet

                        Would you like to open the download page in your browser?
                        """;

        if (interactive)
        {
            if (MessageBox.Confirm("Missing .NET SDK", message))
                openBrowser("https://dotnet.microsoft.com/download/dotnet");
        }
        else
            Console.WriteLine(message);

        Environment.Exit(1); // Signal failure to MSBuild script
    }
    else
    {
        Console.WriteLine("Required .NET SDK is installed.");
        Environment.Exit(0); // Signal success
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    Environment.Exit(1); // Signal failure in case of exceptions
}

return;

Version getInstalledSdkVersion()
{
    var startInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = "--list-sdks",
        RedirectStandardOutput = true,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    using var process = Process.Start(startInfo);
    if (process == null) return noVersion;
    process.WaitForExit();
    var output = process.StandardOutput.ReadToEnd();
    return output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
        .Select(getVersionFromLine)
        .Where(l => l != null)
        .OrderByDescending(v => v) // Get the highest version
        .FirstOrDefault() ?? noVersion;
}

Version? getVersionFromLine(string line)
{
    var endOfVersionIndex = line.LastIndexOf('[');
    if (endOfVersionIndex >= 0)
        line = line[..endOfVersionIndex].Trim();
    if (Version.TryParse(line, out var version))
        return version;
    return noVersion;
}

void showDialog(string title, string message)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        Win32.MessageBox(IntPtr.Zero, message, title, 0);
    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        Process.Start("zenity", $"--error --title=\"{title}\" --text=\"{message}\"");
}

void openBrowser(string url)
{
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to open browser: {ex.Message}");
    }
}
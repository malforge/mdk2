using System.Diagnostics;
using CheckDotNet;

Version noVersion = new(0, 0);
Version requiredVersion = new(9, 0);

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
            if (MessageBox.Confirm($"MDK requires {requiredVersion}", message))
                openBrowser("https://dotnet.microsoft.com/download/dotnet");
        }
        else
            Console.WriteLine(message);

        return 1;
    }

    Console.WriteLine("Required .NET SDK is installed.");
    return 0;
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    return 1;
}

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
using System.Diagnostics;

namespace CheckDotNet;

partial class MessageBox
{
    static bool ConfirmLinux(string title, string message)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "zenity",
            Arguments = $"--question --title=\"{title}\" --text=\"{message}\"",
            RedirectStandardOutput = true,
            UseShellExecute = false
        });
        if (process == null)
            return false;

        process.WaitForExit();
        var exitCode = process.ExitCode;

        return exitCode == 0;
    }
}
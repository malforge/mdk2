using System.Diagnostics;
using System.IO.Compression;

namespace Mdk.DocGen3;

public class SteamCmd
{
    const string SteamCmdZipUrl = "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip";

    const string AppId = "298740";
    public static readonly SteamCmd Instance = new();

    public async Task DownloadOrUpdateGame(string steamCmdInstallPath, string gameInstallPath)
    {
        await DownloadSteamClient(steamCmdInstallPath);

        // Run steamcmd to update or install the game
        var fileName = Path.Combine(steamCmdInstallPath, "steamcmd.exe");
        var arguments = $"+login anonymous +force_install_dir \"{gameInstallPath}\" +app_update {AppId} validate +quit";
        Console.WriteLine($"Running SteamCMD with arguments: {arguments}");

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
            throw new InvalidOperationException("Failed to start SteamCMD process. Ensure steamcmd.exe exists at the specified path.");
        process.OutputDataReceived += (s, e) =>
        {
            if (e.Data != null) Console.WriteLine(e.Data);
        };
        process.ErrorDataReceived += (s, e) =>
        {
            if (e.Data != null) Console.Error.WriteLine(e.Data);
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();
        var exitCode = process.ExitCode;
        if (exitCode != 0)
            throw new InvalidOperationException($"SteamCMD failed with exit code {exitCode}. Check the output for details.");
    }

    static async Task DownloadSteamClient(string steamCmdInstallPath)
    {
        if (File.Exists(Path.Combine(steamCmdInstallPath, "steamcmd.exe")))
            return;

        var steamCmdZipPath = Path.Combine(steamCmdInstallPath, "steamcmd.zip");
        if (!File.Exists(steamCmdZipPath))
        {
            // Make sure the directory exists
            Directory.CreateDirectory(steamCmdInstallPath);
            Console.WriteLine("Downloading SteamCMD...");
            using var client = new HttpClient();
            var response = await client.GetAsync(SteamCmdZipUrl);
            response.EnsureSuccessStatusCode();
            await using var fileStream = new FileStream(steamCmdZipPath, FileMode.Create, FileAccess.Write);
            await response.Content.CopyToAsync(fileStream);
            Console.WriteLine("SteamCMD downloaded.");
        }

        using var archive = ZipFile.OpenRead(steamCmdZipPath);
        foreach (var entry in archive.Entries)
        {
            var destinationPath = Path.Combine(steamCmdInstallPath, entry.FullName);
            if (entry.FullName.EndsWith('/'))
                Directory.CreateDirectory(destinationPath);
            else 
                entry.ExtractToFile(destinationPath, true);
        }

        // Does the steamcmd.exe file exist now?
        if (!File.Exists(Path.Combine(steamCmdInstallPath, "steamcmd.exe")))
            throw new InvalidOperationException("Failed to extract steamcmd.exe from the downloaded zip file.");
    }
}
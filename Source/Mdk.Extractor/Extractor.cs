// Mdk.Extractor
//
// Copyright 2023-2026 The MDK² Authors

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Mdk.Extractor;

/// <summary>
///     Drives a headless Space Engineers dedicated server through one extraction run.
/// </summary>
/// <remarks>
///     The dedicated server creates its world from the configuration file alone, so there is no user interface to
///     drive: write an instance configuration that names a premade world and lists this assembly as a plugin, start
///     the server, and wait for <see cref="ExtractorPlugin" /> to write the files and shut the server down.
///     The plugin runs inside the server process rather than this one, so the output paths are handed over through
///     environment variables the child process inherits.
/// </remarks>
public class Extractor
{
    public const string ModWhitelistVariable = "MDK_EXTRACT_MOD_WHITELIST";
    public const string PbWhitelistVariable = "MDK_EXTRACT_PB_WHITELIST";
    public const string PbPrologueVariable = "MDK_EXTRACT_PB_PROLOGUE";
    public const string TerminalVariable = "MDK_EXTRACT_TERMINAL";

    const string PremadeWorld = "Empty World";
    const string ServerPort = "27019";
    const string SteamPort = "26019";

    public Extractor(string modWhitelist, string pbWhitelist, string pbPrologue, string terminal, string seBinPath, string instancePath, TimeSpan timeout)
    {
        ModWhitelist = modWhitelist;
        PbWhitelist = pbWhitelist;
        PbPrologue = pbPrologue;
        Terminal = terminal;
        SeBinPath = seBinPath;
        InstancePath = instancePath;
        Timeout = timeout;
    }

    public string ModWhitelist { get; }
    public string PbWhitelist { get; }
    public string PbPrologue { get; }
    public string Terminal { get; }
    public string SeBinPath { get; }
    public string InstancePath { get; }
    public TimeSpan Timeout { get; }

    public void Run()
    {
        var executable = Path.Combine(SeBinPath, "SpaceEngineersDedicated.exe");
        if (!File.Exists(executable))
            throw new TerminalException($"Cannot find the dedicated server executable at \"{executable}\"");

        var premade = Path.GetFullPath(Path.Combine(SeBinPath, "..", "Content", "CustomWorlds", PremadeWorld));
        if (!Directory.Exists(premade))
            throw new TerminalException($"Cannot find the premade world the extractor starts from: \"{premade}\"");

        PrepareInstance();
        WriteConfiguration(premade);

        Console.WriteLine($@"Starting the dedicated server from {SeBinPath}");
        var exitCode = RunServer(executable);

        // The server exits on its own once the plugin is done, so a non-zero code is worth reporting, but the
        // files are the real verdict: the server also logs and exits when it cannot create a world at all.
        if (exitCode != 0)
            Console.WriteLine($@"The dedicated server exited with code {exitCode}");

        VerifyOutput();
    }

    void PrepareInstance()
    {
        var directory = new DirectoryInfo(InstancePath);
        if (directory.Exists)
        {
            // The server resumes the previous session if one is left lying around, which would extract from a
            // stale world rather than a freshly created one.
            Cleanup(directory);
        }
        else
            directory.Create();
    }

    void WriteConfiguration(string premadeWorldPath)
    {
        // The plugin has to be loaded from a path the server can see. This assembly is both the driver and the
        // plugin, so it points at itself.
        var pluginPath = new Uri(typeof(Extractor).Assembly.Location).LocalPath;

        // Everything that could spawn something into the world is turned off: the terminal pass spawns its own
        // grids and anything else the world generates is wasted loading time at best.
        var configuration = $@"<?xml version=""1.0"" encoding=""utf-8""?>
<MyConfigDedicated xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <SessionSettings>
    <GameMode>Creative</GameMode>
    <OnlineMode>PRIVATE</OnlineMode>
    <MaxPlayers>4</MaxPlayers>
    <MaxFloatingObjects>0</MaxFloatingObjects>
    <AutoSaveInMinutes>0</AutoSaveInMinutes>
    <EnableSaving>true</EnableSaving>
    <EnableIngameScripts>true</EnableIngameScripts>
    <ProceduralDensity>0</ProceduralDensity>
    <CargoShipsEnabled>false</CargoShipsEnabled>
    <EnableEncounters>false</EnableEncounters>
    <EnableRespawnShips>false</EnableRespawnShips>
    <EnableDrones>false</EnableDrones>
    <MaxDrones>0</MaxDrones>
    <EnableWolfs>false</EnableWolfs>
    <EnableSpiders>false</EnableSpiders>
    <EnableContainerDrops>false</EnableContainerDrops>
    <EnableSunRotation>false</EnableSunRotation>
    <EnableVoxelDestruction>false</EnableVoxelDestruction>
    <FloraDensityMultiplier>0</FloraDensityMultiplier>
  </SessionSettings>
  <LoadWorld />
  <PremadeCheckpointPath>{SecurityElementEscape(premadeWorldPath)}</PremadeCheckpointPath>
  <WorldName>MdkExtract</WorldName>
  <ServerName>MdkExtract</ServerName>
  <IgnoreLastSession>true</IgnoreLastSession>
  <AsteroidAmount>0</AsteroidAmount>
  <PauseGameWhenEmpty>false</PauseGameWhenEmpty>
  <AutoRestartEnabled>false</AutoRestartEnabled>
  <AutoUpdateEnabled>false</AutoUpdateEnabled>
  <RemoteApiEnabled>false</RemoteApiEnabled>
  <IP>0.0.0.0</IP>
  <ServerPort>{ServerPort}</ServerPort>
  <SteamPort>{SteamPort}</SteamPort>
  <Plugins>
    <string>{SecurityElementEscape(pluginPath)}</string>
  </Plugins>
</MyConfigDedicated>
";

        File.WriteAllText(Path.Combine(InstancePath, "SpaceEngineers-Dedicated.cfg"), configuration);
    }

    int RunServer(string executable)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            // -console keeps the server headless; without it the server opens its configuration window when the
            // session is interactive.
            Arguments = $"-console -path \"{InstancePath}\" -ignorelastsession",
            WorkingDirectory = SeBinPath,
            UseShellExecute = false,

            // All three streams have to be redirected. The server finishes with a "press any key to close this
            // window" prompt which it only skips when its input is redirected, so inheriting a real console leaves
            // it sitting there forever after the extraction is already done.
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.EnvironmentVariables[ModWhitelistVariable] = ModWhitelist ?? "";
        startInfo.EnvironmentVariables[PbWhitelistVariable] = PbWhitelist ?? "";
        startInfo.EnvironmentVariables[PbPrologueVariable] = PbPrologue ?? "";
        startInfo.EnvironmentVariables[TerminalVariable] = Terminal ?? "";

        using (var process = Process.Start(startInfo))
        {
            if (process == null)
                throw new TerminalException("Could not start the dedicated server");

            // Relayed rather than swallowed: when a run fails, the server's own log is the only thing that says why.
            process.OutputDataReceived += (_, e) => { if (e.Data != null) Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) Console.Error.WriteLine(e.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.Close();

            if (process.WaitForExit((int)Timeout.TotalMilliseconds))
            {
                // Lets the redirected streams drain before the caller reports anything.
                process.WaitForExit();
                return process.ExitCode;
            }

            Console.WriteLine($@"The dedicated server did not finish within {Timeout}, terminating it");
            try
            {
                process.Kill();
                process.WaitForExit(30000);
            }
            catch (Exception e)
            {
                Console.WriteLine($@"Could not terminate the dedicated server: {e.Message}");
            }

            throw new TerminalException($"The dedicated server did not complete the extraction within {Timeout}");
        }
    }

    void VerifyOutput()
    {
        var missing = new System.Collections.Generic.List<string>();
        foreach (var file in new[] { ModWhitelist, PbWhitelist, PbPrologue, Terminal })
        {
            if (string.IsNullOrEmpty(file))
                continue;
            if (!File.Exists(file))
                missing.Add(file);
        }

        if (missing.Count > 0)
            throw new TerminalException($"The dedicated server finished without writing: {string.Join(", ", missing)}");
    }

    static string SecurityElementEscape(string value) => System.Security.SecurityElement.Escape(value) ?? "";

    void Cleanup(DirectoryInfo directory)
    {
        foreach (var subdirectory in directory.GetDirectories())
        {
            Cleanup(subdirectory);
            try
            {
                subdirectory.Delete();
            }
            catch (Exception)
            {
                Console.WriteLine($@"Unable to clean up {subdirectory.FullName}");
            }
        }

        foreach (var file in directory.GetFiles())
        {
            try
            {
                file.Delete();
            }
            catch (Exception)
            {
                Console.WriteLine($@"Unable to clean up {file.FullName}");
            }
        }
    }
}

namespace Mdk.CommandLine.Shared.Api;

public interface IInteraction
{
    /// <summary>
    /// Show a custom message to the user.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void Custom(string message, params object?[] args);
    
    /// <summary>
    /// Show a notification to the user about a script being successfully deployed.
    /// </summary>
    /// <param name="scriptName"></param>
    /// <param name="folder"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void Script(string scriptName, string folder, string? message = null, params object?[] args);
    
    /// <summary>
    /// Show a notification to the user about a new version of a nuget package being available.
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="currentVersion"></param>
    /// <param name="newVersion"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    void Nuget(string packageName, string currentVersion, string newVersion, string? message = null, params object?[] args);
}

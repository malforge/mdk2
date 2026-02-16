namespace Mdk.CommandLine.CommandLine;

/// <summary>
/// The available verbs for the command line.
/// </summary>
public enum Verb
{
    /// <summary>
    /// No verb specified.
    /// </summary>
    None,
    
    /// <summary>
    /// The help verb shows help information, optionally for a specific verb.
    /// </summary>
    Help,
    
    /// <summary>
    /// The pack verb packages a script or mod into a format the game understands.
    /// </summary>
    Pack
}
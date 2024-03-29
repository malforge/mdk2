namespace Mdk.CommandLine.IngameScript.LegacyConversion;

/// <summary>
/// The minifier level as understood by the legacy minifier.
/// </summary>
public enum LegacyMinifierLevel
{
    /// <summary>
    /// No minification.
    /// </summary>
    None,
    
    /// <summary>
    /// Minify the script by stripping comments.
    /// </summary>
    StripComments,
    
    /// <summary>
    /// Minify the script by stripping comments and whitespace.
    /// </summary>
    Lite,
    
    /// <summary>
    /// Minify the script by stripping comments, whitespace, and shortening variable names.
    /// </summary>
    Full
}
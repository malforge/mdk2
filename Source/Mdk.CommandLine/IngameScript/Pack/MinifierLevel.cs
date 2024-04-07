namespace Mdk.CommandLine.IngameScript.Pack;

/// <summary>
///     The desired level of minification.
/// </summary>
public enum MinifierLevel
{
    /// <summary>
    ///     No minification is requested.
    /// </summary>
    None,

    /// <summary>
    ///     Minify the script by removing unused code.
    /// </summary>
    Trim,

    /// <summary>
    ///     Minify the script by removing unused code and comments.
    /// </summary>
    StripComments,

    /// <summary>
    ///     Perform a light minification of the script, removing unused code, comments, and whitespace.
    /// </summary>
    Lite,

    /// <summary>
    ///     Perform a full minification of the script, removing unused code, comments, whitespace, and renaming variables.
    /// </summary>
    Full
}
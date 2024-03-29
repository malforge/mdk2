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
    ///     Minify the script by removing comments only.
    /// </summary>
    StripComments,

    /// <summary>
    ///     Minify the script by removing comments and whitespace.
    /// </summary>
    Lite,

    /// <summary>
    ///     Perform a full minification of the script, removing comments, whitespace, and shortening variable names.
    /// </summary>
    Full
}
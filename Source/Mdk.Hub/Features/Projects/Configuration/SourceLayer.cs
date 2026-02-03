namespace Mdk.Hub.Features.Projects.Configuration;

/// <summary>
///     Indicates which configuration layer a value came from.
/// </summary>
public enum SourceLayer
{
    /// <summary>
    ///     Default/fallback value (not explicitly set in either file).
    /// </summary>
    Default,

    /// <summary>
    ///     Value from mdk.ini (main project configuration).
    /// </summary>
    Main,

    /// <summary>
    ///     Value from mdk.local.ini (local machine configuration, overrides main).
    /// </summary>
    Local
}

namespace Mdk.CommandLine;

/// <summary>
///     Determines the type of MDK project.
/// </summary>
public enum MdkProjectType
{
    /// <summary>
    ///     The project type is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    ///     This is an in-game script project.
    /// </summary>
    ProgrammableBlock,

    /// <summary>
    ///     This is a mod project.
    /// </summary>
    Mod,

    /// <summary>
    ///     This is a legacy MDK1 programmable block project.
    /// </summary>
    LegacyProgrammableBlock
}
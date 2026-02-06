namespace Mdk.Hub.Features.Projects.Overview;

/// <summary>
///     Specifies the type of MDK project.
/// </summary>
public enum ProjectType
{
    /// <summary>
    ///     A programmable block script that deploys to in-game programmable blocks.
    /// </summary>
    ProgrammableBlock,
    
    /// <summary>
    ///     A full game modification that extends Space Engineers.
    /// </summary>
    Mod
}

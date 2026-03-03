using System;

namespace Mdk.Hub.Features.Projects.Actions;

/// <summary>
///     Describes the visual edge characteristics of an action item for spacing calculations.
/// </summary>
[Flags]
public enum ActionEdgeType
{
    /// <summary>
    ///     The edge is bare - no visual container, needs less spacing.
    /// </summary>
    Bare = 0,

    /// <summary>
    ///     The edge is contained - has a visual container (card, padding), needs more spacing when adjacent to another
    ///     contained edge.
    /// </summary>
    Contained = 1
}
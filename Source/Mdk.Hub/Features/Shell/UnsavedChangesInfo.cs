using System;

namespace Mdk.Hub.Features.Shell;

/// <summary>
///     Information about unsaved changes in the application.
/// </summary>
public readonly struct UnsavedChangesInfo
{
    /// <summary>
    ///     Gets a description of the unsaved changes.
    /// </summary>
    public string Description { get; init; }
    
    /// <summary>
    ///     Gets the action to navigate to the location of the unsaved changes.
    /// </summary>
    public Action GoThereAction { get; init; }
}
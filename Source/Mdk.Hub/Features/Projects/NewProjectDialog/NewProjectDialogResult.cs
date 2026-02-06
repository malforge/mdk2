namespace Mdk.Hub.Features.Projects.NewProjectDialog;

/// <summary>
///     Result data from the new project dialog.
/// </summary>
public readonly struct NewProjectDialogResult
{
    /// <summary>
    ///     Gets the name of the project to create.
    /// </summary>
    public required string ProjectName { get; init; }

    /// <summary>
    ///     Gets the location where the project should be created.
    /// </summary>
    public required string Location { get; init; }
}
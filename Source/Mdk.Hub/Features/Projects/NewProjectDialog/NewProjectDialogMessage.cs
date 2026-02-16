using Mdk.Hub.Features.Projects.Overview;

namespace Mdk.Hub.Features.Projects.NewProjectDialog;

/// <summary>
///     Message data for displaying the new project dialog.
/// </summary>
public readonly struct NewProjectDialogMessage
{
    /// <summary>
    ///     Gets the dialog title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    ///     Gets the dialog message/description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    ///     Gets the icon to display (e.g., ProgrammableBlockSymbol, ModSymbol, MixinSymbol).
    /// </summary>
    public object? Icon { get; init; }

    /// <summary>
    ///     Gets the default project name.
    /// </summary>
    public required string DefaultProjectName { get; init; }

    /// <summary>
    ///     Gets the default location for the new project.
    /// </summary>
    public string DefaultLocation { get; init; }

    /// <summary>
    ///     Gets the OK button text.
    /// </summary>
    public string OkText { get; init; }

    /// <summary>
    ///     Gets the Cancel button text.
    /// </summary>
    public string CancelText { get; init; }
}
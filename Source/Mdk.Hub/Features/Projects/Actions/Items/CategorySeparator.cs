using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions.Items;

/// <summary>
///     Represents a visual separator between action categories.
/// </summary>
[ViewModelFor<CategorySeparatorView>]
public class CategorySeparator : ActionItem
{
    /// <summary>
    ///     Gets the category this separator belongs to (null for separators).
    /// </summary>
    public override string? Category => null;

    /// <summary>
    ///     Determines whether this separator should be shown.
    /// </summary>
    /// <returns>Always returns true; visibility is controlled by the ViewModel.</returns>
    public override bool ShouldShow() => true; // Separator visibility is controlled by ViewModel
}

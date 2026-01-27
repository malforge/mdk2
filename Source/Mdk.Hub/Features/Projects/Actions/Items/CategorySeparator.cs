using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.Projects.Actions.Items;

[ViewModelFor<CategorySeparatorView>]
public class CategorySeparator : ActionItem
{
    public override string? Category => null;

    public override bool ShouldShow() => true; // Separator visibility is controlled by ViewModel
}
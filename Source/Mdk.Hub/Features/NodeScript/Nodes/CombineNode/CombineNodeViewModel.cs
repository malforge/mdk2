using System.Collections.Generic;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     ViewModel for a Combine node — merges multiple block sources into a single source.
/// </summary>
[ViewModelFor<CombineNodeView>]
public class CombineNodeViewModel : SimpleNodeViewModel
{
    /// <summary>Initializes a new instance with two default input connectors.</summary>
    public CombineNodeViewModel()
    {
        Inputs = [new ConnectorViewModel(), new ConnectorViewModel()];
        Outputs = [new ConnectorViewModel { IsOutput = true }];
    }

    /// <inheritdoc />
    public override string Title => "Combine";

    /// <summary>Gets the input connectors (block sources to merge).</summary>
    public IReadOnlyList<ConnectorViewModel> Inputs { get; }

    /// <summary>Gets the output connector (the merged block collection).</summary>
    public IReadOnlyList<ConnectorViewModel> Outputs { get; }
}

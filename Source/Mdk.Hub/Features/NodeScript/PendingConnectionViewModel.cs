using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     View model for the in-progress connection being dragged from a connector.
/// </summary>
public class PendingConnectionViewModel : ViewModel
{
    ConnectorViewModel? _source;
    readonly NodeScriptEditorViewModel _editor;

    /// <summary>
    ///     Initializes a new instance of <see cref="PendingConnectionViewModel" />.
    /// </summary>
    public PendingConnectionViewModel(NodeScriptEditorViewModel editor)
    {
        _editor = editor;
        StartCommand = new RelayCommand<ConnectorViewModel>(c => Source = c);
        FinishCommand = new RelayCommand<ConnectorViewModel>(Finish);
    }

    /// <summary>Gets or sets the connector that started the drag.</summary>
    public ConnectorViewModel? Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    /// <summary>Gets the command invoked when a connector starts being dragged.</summary>
    public RelayCommand<ConnectorViewModel> StartCommand { get; }

    /// <summary>Gets the command invoked when the drag is released on a target connector.</summary>
    public RelayCommand<ConnectorViewModel> FinishCommand { get; }

    void Finish(ConnectorViewModel? target)
    {
        if (target == null || target == Source || Source == null) return;
        _editor.Connect(Source, target);
        Source = null;
    }
}

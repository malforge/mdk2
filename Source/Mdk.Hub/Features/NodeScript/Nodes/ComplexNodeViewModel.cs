using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript.Nodes;

/// <summary>
///     Base class for nodes with an expandable inline editor (header + strip + collapsed summary or expanded fields).
/// </summary>
public abstract class ComplexNodeViewModel : SimpleNodeViewModel
{
    bool _isExpanded;

    /// <summary>Initializes a new <see cref="ComplexNodeViewModel"/>.</summary>
    protected ComplexNodeViewModel()
    {
        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
    }

    /// <summary>Gets or sets whether the node is in expanded (edit) mode.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (SetProperty(ref _isExpanded, value))
                OnPropertyChanged(nameof(ChevronGlyph));
        }
    }

    /// <summary>Gets the chevron glyph reflecting the current expanded state.</summary>
    public string ChevronGlyph => _isExpanded ? "▼" : "▶";

    /// <summary>Gets the command that toggles expanded/collapsed state.</summary>
    public RelayCommand ToggleExpandCommand { get; }

    /// <summary>Gets a compact one-line summary of the node's current configuration for the collapsed view.</summary>
    public abstract string Summary { get; }
}

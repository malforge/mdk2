using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.Input;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     ViewModel for the node-based script editor.
/// </summary>
[Instance]
[ViewModelFor<NodeScriptEditorView>]
public partial class NodeScriptEditorViewModel : ViewModel, ISupportClosing, IHaveATitle
{
    string _title = "Node Script Editor";
    double _zoom = 1.0;
    Point _addNodeMenuPosition;
    bool _isAddNodeMenuOpen;
    AddNodeMenuViewModel? _addNodeMenu;

    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeScriptEditorViewModel" /> class.
    /// </summary>
    public NodeScriptEditorViewModel()
    {
        Nodes = new ObservableCollection<object>();
        Connections = new ObservableCollection<object>();
    }

    /// <summary>
    ///     Gets or sets the editor title.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    /// <summary>
    ///     Gets the collection of nodes in the editor.
    /// </summary>
    public ObservableCollection<object> Nodes { get; }

    /// <summary>
    ///     Gets the collection of connections between nodes.
    /// </summary>
    public ObservableCollection<object> Connections { get; }

    /// <summary>
    ///     Gets whether the editor has any nodes.
    /// </summary>
    public bool HasNodes => Nodes.Count > 0;

    /// <summary>
    ///     Gets or sets the viewport zoom level.
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    /// <summary>
    ///     Gets or sets whether the add node menu is open.
    /// </summary>
    public bool IsAddNodeMenuOpen
    {
        get => _isAddNodeMenuOpen;
        set => SetProperty(ref _isAddNodeMenuOpen, value);
    }

    /// <summary>
    ///     Gets or sets the add node menu position.
    /// </summary>
    public Point AddNodeMenuPosition
    {
        get => _addNodeMenuPosition;
        set => SetProperty(ref _addNodeMenuPosition, value);
    }

    /// <summary>
    ///     Gets the add node menu view model.
    /// </summary>
    public AddNodeMenuViewModel? AddNodeMenu
    {
        get => _addNodeMenu;
        set => SetProperty(ref _addNodeMenu, value);
    }

    /// <summary>
    ///     Opens the add node menu at the specified position.
    /// </summary>
    public void OpenAddNodeMenu(Point position)
    {
        AddNodeMenuPosition = position;
        
        var menu = new AddNodeMenuViewModel { Position = position };
        menu.NodeCreated += nodeType =>
        {
            CreateNode(nodeType, position);
            CloseAddNodeMenu();
        };
        menu.Cancelled += CloseAddNodeMenu;
        
        AddNodeMenu = menu;
        IsAddNodeMenuOpen = true;
    }

    /// <summary>
    ///     Closes the add node menu and cleans up.
    /// </summary>
    void CloseAddNodeMenu()
    {
        IsAddNodeMenuOpen = false;
        AddNodeMenu = null;
    }

    /// <summary>
    ///     Creates a node of the specified type at the given position.
    /// </summary>
    void CreateNode(string nodeType, Point position)
    {
        switch (nodeType)
        {
            case "Block":
                var blockNode = new BlockNodeViewModel { Location = position };
                Nodes.Add(blockNode);
                break;
            
            // TODO: Add other node types (OnArgument, WaitForState, etc.)
        }
        
        OnPropertyChanged(nameof(HasNodes));
    }

    /// <inheritdoc />
    public Task<bool> WillCloseAsync() =>
        // For now, always allow closing
        // TODO: Check for unsaved changes and prompt user
        Task.FromResult(true);

    /// <inheritdoc />
    public Task DidCloseAsync() => Task.CompletedTask;
}
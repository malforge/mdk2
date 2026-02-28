using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.NodeScript.BlockSelector;
using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     ViewModel for the node-based script editor.
/// </summary>
[Instance]
[ViewModelFor<NodeScriptEditorView>]
public partial class NodeScriptEditorViewModel : ViewModel, ISupportClosing, IHaveATitle, Shell.IFileEditor
{
    readonly IShell _shell;
    string _title = "Node Script Editor";
    string? _filePath;
    double _zoom = 1.0;
    Point _addNodeMenuPosition;
    bool _isAddNodeMenuOpen;
    AddNodeMenuViewModel? _addNodeMenu;

    /// <summary>
    ///     Design-time constructor.
    /// </summary>
    public NodeScriptEditorViewModel() : this(null!) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeScriptEditorViewModel" /> class.
    /// </summary>
    public NodeScriptEditorViewModel(IShell shell)
    {
        _shell = shell;
        Nodes = new ObservableCollection<object>();
        Connections = new ObservableCollection<object>();
        OverlayViews = new ObservableCollection<object>();
        TestBlockSelectorCommand = new AsyncRelayCommand(TestBlockSelectorAsync);
    }

    /// <inheritdoc />
    public async Task OpenFileAsync(string filePath)
    {
        _filePath = filePath;
        OnPropertyChanged(nameof(FilePath));
        UpdateTitle();
        
        // TODO: Load the file content when file loading is implemented
        await Task.CompletedTask;
    }
    
    /// <summary>
    ///     Gets the file path for this node script, or null if this is a new unsaved script.
    /// </summary>
    public string? FilePath => _filePath;

    /// <summary>
    ///     Gets or sets the editor title.
    /// </summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    /// <summary>
    ///     Updates the title based on the current file path.
    /// </summary>
    void UpdateTitle()
    {
        if (string.IsNullOrEmpty(_filePath))
            Title = "Node Script Editor";
        else
            Title = $"Node Script Editor - {System.IO.Path.GetFileName(_filePath)}";
    }

    /// <summary>
    ///     Gets the command that opens the block selector overlay for testing.
    /// </summary>
    public AsyncRelayCommand TestBlockSelectorCommand { get; }

    /// <summary>
    ///     Gets the collection of nodes in the editor.
    /// </summary>
    public ObservableCollection<object> Nodes { get; }

    /// <summary>
    ///     Gets the collection of connections between nodes.
    /// </summary>
    public ObservableCollection<object> Connections { get; }

    /// <summary>
    ///     Gets the collection of overlay views (dialogs).
    /// </summary>
    public ObservableCollection<object> OverlayViews { get; }

    /// <summary>
    ///     Gets whether any overlays are open.
    /// </summary>
    public bool HasOverlays => OverlayViews.Count > 0;

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
            case "Blocks":
                var blocksNode = new BlocksNodeViewModel { Location = position };
                Nodes.Add(blocksNode);
                break;
            
            // TODO: Add other node types (OnArgument, WaitForState, etc.)
        }
        
        OnPropertyChanged(nameof(HasNodes));
    }
    
    /// <summary>
    ///     Opens the property editor for a node.
    /// </summary>
    public void OpenNodeEditor(object nodeViewModel)
    {
        if (nodeViewModel is not INodeEditor editable)
            return;

        var (editorViewModel, editorViewType) = editable.GetEditor();
        
        // Create the view instance
        var editorView = Activator.CreateInstance(editorViewType);
        if (editorView is not Avalonia.Controls.Control control)
            return;

        control.DataContext = editorViewModel;

        // Hook up close event if the editor supports it
        if (editorViewModel is Editors.BlocksNodeEditorViewModel blocksEditor)
        {
            blocksEditor.CloseRequested += () =>
            {
                OverlayViews.Remove(control);
                OnPropertyChanged(nameof(HasOverlays));
            };
        }

        OverlayViews.Add(control);
        OnPropertyChanged(nameof(HasOverlays));
    }
    
    /// <inheritdoc />
    public Task<bool> WillCloseAsync() =>
        // For now, always allow closing
        // TODO: Check for unsaved changes and prompt user
        Task.FromResult(true);

    /// <inheritdoc />
    public Task DidCloseAsync() => Task.CompletedTask;

    async Task TestBlockSelectorAsync()
    {
        var selector = new BlockSelectorViewModel();
        var view = new BlockSelector.BlockSelectorView { DataContext = selector };

        var tcs = new TaskCompletionSource();
        selector.Dismissed += (_, _) =>
        {
            OverlayViews.Remove(view);
            OnPropertyChanged(nameof(HasOverlays));
            tcs.TrySetResult();
        };

        OverlayViews.Add(view);
        OnPropertyChanged(nameof(HasOverlays));

        await tcs.Task;
    }
}
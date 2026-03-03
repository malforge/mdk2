using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Mal.SourceGeneratedDI;
using Mdk.Hub.Features.NodeScript.BlockSelector;
using Mdk.Hub.Features.NodeScript.Editors;
using Mdk.Hub.Features.NodeScript.NodeSelector;
using Mdk.Hub.Features.NodeScript.Nodes;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     ViewModel for the node-based script editor.
/// </summary>
[Instance(Container = "Window")]
[ViewModelFor<NodeScriptEditorView>]
public partial class NodeScriptEditorViewModel : ViewModel, ISupportClosing, IHaveATitle, Shell.IFileEditor
{
    readonly IShell _shell;
    readonly IOverlayService _overlayService;
    readonly IBlockPickerService _blockPicker;
    readonly IDependencyContainer _container;
    string _title = "Node Script Editor";
    string? _filePath;
    double _zoom = 1.0;

    /// <summary>
    ///     Design-time constructor.
    /// </summary>
    public NodeScriptEditorViewModel() : this(null!, null!, null!, null!) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NodeScriptEditorViewModel" /> class.
    /// </summary>
    public NodeScriptEditorViewModel(IShell shell, IOverlayService overlayService, IBlockPickerService blockPicker, IDependencyContainer container)
    {
        _shell = shell;
        _overlayService = overlayService;
        _blockPicker = blockPicker;
        _container = container;
        Nodes = new ObservableCollection<object>();
        Connections = new ObservableCollection<object>();
        _overlayService.Views.CollectionChanged += (_, _) => OnPropertyChanged(nameof(HasOverlays));
        TestBlockSelectorCommand = new AsyncRelayCommand(TestBlockSelectorAsync);
    }

    /// <inheritdoc />
    public async Task OpenFileAsync(string filePath)
    {
        _filePath = filePath;
        OnPropertyChanged(nameof(FilePath));
        UpdateTitle();
        await Task.CompletedTask;
    }

    /// <summary>Gets the file path for this node script, or null if this is a new unsaved script.</summary>
    public string? FilePath => _filePath;

    /// <summary>Gets or sets the editor title.</summary>
    public string Title
    {
        get => _title;
        private set => SetProperty(ref _title, value);
    }

    void UpdateTitle()
    {
        Title = string.IsNullOrEmpty(_filePath)
            ? "Node Script Editor"
            : $"Node Script Editor - {System.IO.Path.GetFileName(_filePath)}";
    }

    /// <summary>Gets the command that opens the block selector overlay for testing.</summary>
    public AsyncRelayCommand TestBlockSelectorCommand { get; }

    /// <summary>Gets the collection of nodes in the editor.</summary>
    public ObservableCollection<object> Nodes { get; }

    /// <summary>Gets the collection of connections between nodes.</summary>
    public ObservableCollection<object> Connections { get; }

    /// <summary>Gets the overlay view model collection (bound by the editor view).</summary>
    public ObservableCollection<object> OverlayViews => _overlayService.Views;

    /// <summary>Gets whether any overlays are currently shown.</summary>
    public bool HasOverlays => _overlayService.HasViews;

    /// <summary>Gets whether the editor has any nodes.</summary>
    public bool HasNodes => Nodes.Count > 0;

    /// <summary>Gets or sets the viewport zoom level.</summary>
    public double Zoom
    {
        get => _zoom;
        set => SetProperty(ref _zoom, value);
    }

    /// <summary>Opens the node selector overlay at the specified canvas position.</summary>
    public void OpenAddNodeMenu(Point position)
    {
        var selector = _container.Resolve<NodeSelectorViewModel>();
        selector.Dismissed += (_, _) =>
        {
            if (selector.SelectedNodeTypeId is { } nodeType)
                CreateNode(nodeType, position);
        };
        _overlayService.Show(selector);
    }

    void CreateNode(string nodeType, Point position)
    {
        switch (nodeType)
        {
            case "Blocks":
                Nodes.Add(new BlocksNodeViewModel { Location = position });
                break;
        }
        OnPropertyChanged(nameof(HasNodes));
    }

    /// <summary>Opens the property editor for a node.</summary>
    public void OpenNodeEditor(object nodeViewModel)
    {
        if (nodeViewModel is BlocksNodeViewModel blocksNode)
            _overlayService.Show(new BlocksNodeEditorViewModel(blocksNode, _blockPicker, _overlayService));
    }

    /// <inheritdoc />
    public Task<bool> WillCloseAsync() => Task.FromResult(true);

    /// <inheritdoc />
    public Task DidCloseAsync() => Task.CompletedTask;

    async Task TestBlockSelectorAsync() => _ = await _blockPicker.PickAsync(_overlayService);
}

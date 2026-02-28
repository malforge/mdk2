using System;
using Avalonia;
using Avalonia.Controls;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     ViewModel for the add node menu overlay.
/// </summary>
public class AddNodeMenuViewModel : ViewModel
{
    Point _position;
    
    /// <summary>
    ///     Initializes a new instance of the <see cref="AddNodeMenuViewModel"/> class.
    /// </summary>
    public AddNodeMenuViewModel()
    {
        CreateBlocksNodeCommand = new RelayCommand(CreateBlocksNode);
        CreateOnArgumentNodeCommand = new RelayCommand(CreateOnArgumentNode);
        CreateWaitForStateNodeCommand = new RelayCommand(CreateWaitForStateNode);
        CancelCommand = new RelayCommand(Cancel);
    }
    
    /// <summary>
    ///     Gets or sets the position where the menu should appear.
    /// </summary>
    public Point Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    /// <summary>
    ///     Gets the command to create a Blocks node.
    /// </summary>
    public RelayCommand CreateBlocksNodeCommand { get; }

    /// <summary>
    ///     Gets the command to create an OnArgument node.
    /// </summary>
    public RelayCommand CreateOnArgumentNodeCommand { get; }

    /// <summary>
    ///     Gets the command to create a WaitForState node.
    /// </summary>
    public RelayCommand CreateWaitForStateNodeCommand { get; }

    /// <summary>
    ///     Gets the command to cancel the menu.
    /// </summary>
    public RelayCommand CancelCommand { get; }

    void CreateBlocksNode()
    {
        NodeCreated?.Invoke("Blocks");
    }

    void CreateOnArgumentNode()
    {
        NodeCreated?.Invoke("OnArgument");
    }

    void CreateWaitForStateNode()
    {
        NodeCreated?.Invoke("WaitForState");
    }

    void Cancel()
    {
        Cancelled?.Invoke();
    }

    /// <summary>
    ///     Event raised when a node type is selected.
    ///     Parameter is the node type name.
    /// </summary>
    public event Action<string>? NodeCreated;

    /// <summary>
    ///     Event raised when the menu is cancelled.
    /// </summary>
    public event Action? Cancelled;
}

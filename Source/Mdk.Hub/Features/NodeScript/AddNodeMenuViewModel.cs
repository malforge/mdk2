using System;
using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.NodeScript;

/// <summary>
///     ViewModel for the add node menu overlay.
/// </summary>
public partial class AddNodeMenuViewModel : ViewModel
{
    Point _position;
    
    /// <summary>
    ///     Gets or sets the position where the menu should appear.
    /// </summary>
    public Point Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    /// <summary>
    ///     Creates a Block node (data source).
    /// </summary>
    [RelayCommand]
    void CreateBlockNode()
    {
        NodeCreated?.Invoke("Block");
    }

    /// <summary>
    ///     Creates an OnArgument trigger node.
    /// </summary>
    [RelayCommand]
    void CreateOnArgumentNode()
    {
        NodeCreated?.Invoke("OnArgument");
    }

    /// <summary>
    ///     Creates a WaitForState flow control node.
    /// </summary>
    [RelayCommand]
    void CreateWaitForStateNode()
    {
        NodeCreated?.Invoke("WaitForState");
    }

    /// <summary>
    ///     Closes the menu without creating a node.
    /// </summary>
    [RelayCommand]
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

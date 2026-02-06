using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     Overlay view displaying a busy indicator with optional progress bar.
/// </summary>
[Singleton]
public partial class BusyOverlayView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the BusyOverlayView class.
    /// </summary>
    public BusyOverlayView()
    {
        InitializeComponent();
    }
}

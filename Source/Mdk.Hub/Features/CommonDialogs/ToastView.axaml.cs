using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     View for displaying brief toast notification messages.
/// </summary>
[Instance]
public partial class ToastView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the ToastView class.
    /// </summary>
    public ToastView()
    {
        InitializeComponent();
    }
}
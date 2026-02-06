using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     Generic message box dialog for confirmations and information messages.
/// </summary>
[Instance]
public partial class MessageBoxView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the MessageBoxView class.
    /// </summary>
    public MessageBoxView()
    {
        InitializeComponent();
    }
}

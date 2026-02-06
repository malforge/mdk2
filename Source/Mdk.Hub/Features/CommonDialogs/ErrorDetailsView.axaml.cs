using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     Dialog view for displaying detailed error information with expandable details.
/// </summary>
[Instance]
public partial class ErrorDetailsView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the ErrorDetailsView class.
    /// </summary>
    public ErrorDetailsView()
    {
        InitializeComponent();
    }
}

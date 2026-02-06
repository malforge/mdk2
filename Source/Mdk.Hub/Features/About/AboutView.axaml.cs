using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.About;

/// <summary>
///     View displaying application information, version, and credits.
/// </summary>
[Instance]
public partial class AboutView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the AboutView class.
    /// </summary>
    public AboutView()
    {
        InitializeComponent();
    }
}

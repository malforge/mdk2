using Avalonia.Controls;
using Mal.SourceGeneratedDI;

namespace Mdk.Hub.Features.Projects.Options;

/// <summary>
///     View for editing project-specific configuration options.
/// </summary>
[Singleton]
public partial class ProjectOptionsView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the ProjectOptionsView class.
    /// </summary>
    public ProjectOptionsView()
    {
        InitializeComponent();
    }
}

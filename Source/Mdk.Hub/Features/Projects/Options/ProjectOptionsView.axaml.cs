using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Projects.Options;

[Singleton]
public partial class ProjectOptionsView : UserControl
{
    public ProjectOptionsView()
    {
        InitializeComponent();
    }
}

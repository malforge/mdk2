using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JetBrains.Annotations;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Projects.Overview;

[Dependency]
[UsedImplicitly]
public partial class ProjectOverviewView : UserControl
{
    public ProjectOverviewView()
    {
        InitializeComponent();
    }
}
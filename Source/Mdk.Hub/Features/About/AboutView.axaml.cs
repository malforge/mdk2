using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.About;

[Dependency]
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }
}
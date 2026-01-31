using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.About;

[Instance]
public partial class AboutView : UserControl
{
    public AboutView()
    {
        InitializeComponent();
    }
}
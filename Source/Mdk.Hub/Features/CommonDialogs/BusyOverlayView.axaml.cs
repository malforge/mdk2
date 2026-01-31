using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.CommonDialogs;

[Singleton]
public partial class BusyOverlayView : UserControl
{
    public BusyOverlayView()
    {
        InitializeComponent();
    }
}
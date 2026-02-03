using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.CommonDialogs;

[Instance]
public partial class ErrorDetailsView : UserControl
{
    public ErrorDetailsView()
    {
        InitializeComponent();
    }
}

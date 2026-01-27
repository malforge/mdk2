using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.CommonDialogs;

[Dependency]
public partial class MessageBoxView : UserControl
{
    public MessageBoxView()
    {
        InitializeComponent();
    }
}
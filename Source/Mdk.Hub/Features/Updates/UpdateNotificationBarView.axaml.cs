using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Updates;

[Singleton]
public partial class UpdateNotificationBarView : UserControl
{
    public UpdateNotificationBarView()
    {
        InitializeComponent();
    }
}
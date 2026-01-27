using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Updates;

[Dependency]
public partial class UpdateNotificationBarView : UserControl
{
    public UpdateNotificationBarView()
    {
        InitializeComponent();
    }
}
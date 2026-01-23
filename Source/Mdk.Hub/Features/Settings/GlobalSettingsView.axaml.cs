using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Settings;

[Dependency<GlobalSettingsView>]
public partial class GlobalSettingsView : UserControl
{
    public GlobalSettingsView()
    {
        InitializeComponent();
    }
}

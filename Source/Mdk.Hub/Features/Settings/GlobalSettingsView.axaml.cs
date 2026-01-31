using Avalonia.Controls;
using Mal.DependencyInjection;

namespace Mdk.Hub.Features.Settings;

[Singleton<GlobalSettingsView>]
public partial class GlobalSettingsView : UserControl
{
    public GlobalSettingsView()
    {
        InitializeComponent();
    }
}
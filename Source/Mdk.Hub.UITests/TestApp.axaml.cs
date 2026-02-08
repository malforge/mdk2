using Avalonia;
using Avalonia.Markup.Xaml;

namespace Mdk.Hub.UITests;

public class TestApp : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }
}

using Avalonia;
using Avalonia.Headless;
using Mdk.Hub.UITests;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace Mdk.Hub.UITests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        IconProvider.Current.Register<FontAwesomeIconProvider>();

        return AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
    }
}

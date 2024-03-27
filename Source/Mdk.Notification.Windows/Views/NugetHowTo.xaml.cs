using System.Diagnostics;
using System.Windows;

namespace Mdk.Notification.Windows.Views;

public partial class NugetHowTo : Window
{
    const string MdkSite = "https://github.com/malware-dev/MDK-SE/issues";

    public NugetHowTo()
    {
        InitializeComponent();
        DataContext = new HugetHowToModel();
    }

    void CloseButton_OnClick(object sender, RoutedEventArgs e) => Close();

    void MdkSiteLink_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(MdkSite);
        }
        catch
        {
            // ignored
        }
    }
}
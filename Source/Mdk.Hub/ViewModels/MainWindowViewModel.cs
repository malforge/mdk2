using System;
using System.Threading.Tasks;
using Mdk.Hub.Services;

namespace Mdk.Hub.ViewModels;

public class MainWindowViewModel : ViewModelBase, IApplication
{
    public void DidMinify()
    {
        
    }

    public bool WillClose()
    {
        return true;
    }

    public void DidClose()
    {
        
    }

    public async Task RequestCloseAsync()
    {
        
    }
}
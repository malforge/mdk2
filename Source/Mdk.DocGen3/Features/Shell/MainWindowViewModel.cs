using System.Collections.ObjectModel;
using Mdk.DocGen3.Features.ApiGenerator;

namespace Mdk.DocGen3.Features.Shell;

public class MainWindowViewModel : ViewModelBase
{
    public MainWindowViewModel() { }

    public MainWindowViewModel(
        ApiGeneratorViewModel apiGeneratorViewModel
    )
    {
        Items.Add(apiGeneratorViewModel);
    }

    public string Greeting { get; } = "Welcome to Avalonia!";

    public ObservableCollection<ItemViewModel> Items { get; } = new();
}
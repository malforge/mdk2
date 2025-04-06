using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace Mdk.Notification.ViewModels;

public readonly struct ToastAction
{
    public ToastAction(string text, Action<Toast?> action)
    {
        Text = text;
        Command = new RelayCommand<Toast>(action);
    }

    public ToastAction(string text, Func<Toast?, Task> action)
    {
        Text = text;
        Command = new AsyncRelayCommand<Toast>(action);
    }

    public string Text { get; }
    public ICommand Command { get; }
}
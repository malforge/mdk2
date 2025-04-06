using System.Collections.Immutable;

namespace Mdk.Notification.ViewModels;

public class MessageToast(string message, ImmutableArray<ToastAction> actions = default) : Toast
{
    public string Message { get; } = message;
    public ImmutableArray<ToastAction> Actions { get; } = actions.IsDefaultOrEmpty ? ImmutableArray<ToastAction>.Empty : actions;
}
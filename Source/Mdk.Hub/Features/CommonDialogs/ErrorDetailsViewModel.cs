using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

[ViewModelFor<ErrorDetailsView>]
public class ErrorDetailsViewModel : OverlayModel
{
    readonly AsyncRelayCommand _copyToClipboardCommand;
    readonly RelayCommand _dismissCommand;

    public ErrorDetailsViewModel()
    {
        _dismissCommand = new RelayCommand(() => Dismiss());
        _copyToClipboardCommand = new AsyncRelayCommand(CopyToClipboardAsync);
    }

    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string Details { get; init; }

    public ICommand DismissCommand => _dismissCommand;
    public ICommand CopyToClipboardCommand => _copyToClipboardCommand;

    async Task CopyToClipboardAsync()
    {
        var topLevel = Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? TopLevel.GetTopLevel(desktop.MainWindow)
            : null;

        if (topLevel?.Clipboard != null)
            await topLevel.Clipboard.SetTextAsync(Details);
    }
}
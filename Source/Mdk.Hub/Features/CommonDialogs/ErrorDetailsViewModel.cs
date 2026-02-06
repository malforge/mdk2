using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Mdk.Hub.Features.Shell;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
/// View model for displaying error details in an overlay dialog.
/// </summary>
[ViewModelFor<ErrorDetailsView>]
public class ErrorDetailsViewModel : OverlayModel
{
    readonly AsyncRelayCommand _copyToClipboardCommand;
    readonly RelayCommand _dismissCommand;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorDetailsViewModel"/> class.
    /// </summary>
    public ErrorDetailsViewModel()
    {
        _dismissCommand = new RelayCommand(() => Dismiss());
        _copyToClipboardCommand = new AsyncRelayCommand(CopyToClipboardAsync);
    }

    /// <summary>
    /// Gets or initializes the title of the error dialog.
    /// </summary>
    public required string Title { get; init; }
    
    /// <summary>
    /// Gets or initializes the error message to display.
    /// </summary>
    public required string Message { get; init; }
    
    /// <summary>
    /// Gets or initializes the detailed error information.
    /// </summary>
    public required string Details { get; init; }

    /// <summary>
    /// Gets the command to dismiss the error dialog.
    /// </summary>
    public ICommand DismissCommand => _dismissCommand;
    
    /// <summary>
    /// Gets the command to copy error details to the clipboard.
    /// </summary>
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

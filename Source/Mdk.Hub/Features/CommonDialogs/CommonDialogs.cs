using System.Threading.Tasks;
using Mal.DependencyInjection;
using Mdk.Hub.Features.Shell;

namespace Mdk.Hub.Features.CommonDialogs;

[Dependency<ICommonDialogs>]
public class CommonDialogs(IShell shell) : ICommonDialogs
{
    readonly IShell _shell = shell;

    public async Task<bool> ShowAsync(ConfirmationMessage message)
    {
        var model = new MessageBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true
                },
                new MessageBoxChoice
                {
                    Text = message.CancelText,
                    Value = false
                }
            ]
        };

        await _shell.ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    public async Task ShowAsync(InformationMessage message)
    {
        var model = new MessageBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true,
                    IsDefault = true
                }
            ]
        };

        await _shell.ShowOverlayAsync(model);
    }

    public async Task<bool> ShowAsync(KeyPhraseValidationMessage message)
    {
        var model = new DangerBoxViewModel
        {
            Title = message.Title,
            Message = message.Message,
            RequiredKeyPhrase = message.RequiredKeyPhrase,
            KeyPhraseWatermark = message.KeyPhraseWatermark,
            Choices =
            [
                new DangerousMessageBoxChoice
                {
                    Text = message.OkText,
                    Value = true,
                    IsDefault = true
                },
                new MessageBoxChoice
                {
                    Text = message.CancelText,
                    Value = false,
                    IsCancel = true
                }
            ]
        };

        await _shell.ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    public void ShowToast(string message, int durationMs = 3000) => _shell.ShowToast(message, durationMs);
}
using System;
using System.Threading.Tasks;
using Mdk.Hub.Features.CommonDialogs;

namespace Mdk.Hub.Features.Shell;

public static class ShellExtensions
{
    public static Task ShowOverlayAsync(this IShell shell, OverlayModel model)
    {
        var tcs = new TaskCompletionSource();

        void handler(object? sender, EventArgs e)
        {
            model.Dismissed -= handler;
            tcs.SetResult();
        }

        model.Dismissed += handler;
        shell.AddOverlay(model);
        return tcs.Task;
    }

    public static async Task<bool> ConfirmDangerousOperationAsync(this IShell shell, string title, string message, string keyPhraseWatermark, string requiredKeyPhrase, string okText = "OK", string cancelText = "Cancel")
    {
        if (string.IsNullOrEmpty(title)) throw new ArgumentException(@"Value cannot be null or empty.", nameof(title));
        if (string.IsNullOrEmpty(message)) throw new ArgumentException(@"Value cannot be null or empty.", nameof(message));
        if (string.IsNullOrEmpty(keyPhraseWatermark)) throw new ArgumentException(@"Value cannot be null or empty.", nameof(keyPhraseWatermark));
        if (string.IsNullOrEmpty(requiredKeyPhrase)) throw new ArgumentException(@"Value cannot be null or empty.", nameof(requiredKeyPhrase));
        var model = new DangerBoxViewModel
        {
            Title = title,
            Message = message,
            RequiredKeyPhrase = requiredKeyPhrase,
            KeyPhraseWatermark = keyPhraseWatermark,
            Choices =
            [
                new DangerousMessageBoxChoice
                {
                    Text = okText,
                    Value = true,
                    IsDefault = true
                },
                new MessageBoxChoice
                {
                    Text = cancelText,
                    Value = false,
                    IsCancel = true
                }
            ]
        };

        await shell.ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }

    public static async Task<bool> ConfirmAsync(this IShell shell, string title, string message, string okText = "OK", string cancelText = "Cancel")
    {
        var model = new MessageBoxViewModel
        {
            Title = title,
            Message = message,
            Choices =
            [
                new MessageBoxChoice
                {
                    Text = okText,
                    Value = true
                },
                new MessageBoxChoice
                {
                    Text = cancelText,
                    Value = false
                }
            ]
        };

        await shell.ShowOverlayAsync(model);
        return (bool)(model.SelectedValue ?? false);
    }
}
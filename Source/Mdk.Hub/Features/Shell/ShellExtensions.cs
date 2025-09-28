using System;
using System.Threading.Tasks;

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
}
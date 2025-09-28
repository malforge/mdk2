using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

public class ShakingBehavior(Control control) : Behavior(control)
{
    readonly Animation _shakingAnimation = new()
    {
        Duration = TimeSpan.FromMilliseconds(300),
        FillMode = FillMode.Forward,
        Children =
        {
            new KeyFrame
            {
                Cue = new Cue(0.2),
                Setters = { new Setter(TranslateTransform.XProperty, -10d) }
            },
            new KeyFrame
            {
                Cue = new Cue(0.4),
                Setters = { new Setter(TranslateTransform.XProperty, 10d) }
            },
            new KeyFrame
            {
                Cue = new Cue(0.6),
                Setters = { new Setter(TranslateTransform.XProperty, -10d) }
            },
            new KeyFrame
            {
                Cue = new Cue(0.8),
                Setters = { new Setter(TranslateTransform.XProperty, 10d) }
            },
            new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(TranslateTransform.XProperty, 0d) }
            }
        }
    };

    CancellationTokenSource? _cancelAnimation;

    public async Task ShakeAsync()
    {
        var control = Control;
        if (control.RenderTransform is not TranslateTransform)
            control.RenderTransform = new TranslateTransform();

        if (_cancelAnimation is not null)
        {
            await _cancelAnimation.CancelAsync();
            _cancelAnimation = null;
        }

        _cancelAnimation = new CancellationTokenSource();
        // ConfigureAwait(false) simply because this is the last await in the method
        // and we no longer need to be on the UI thread. Make sure to change this if
        // more code is added after this point which needs to be on the UI thread.
        await _shakingAnimation.RunAsync(control, _cancelAnimation.Token).ConfigureAwait(false);
    }

    protected override void OnControlUnloaded()
    {
        if (_cancelAnimation is not null)
        {
            _cancelAnimation.Cancel();
            _cancelAnimation = null;
        }
        base.OnControlUnloaded();
    }
}
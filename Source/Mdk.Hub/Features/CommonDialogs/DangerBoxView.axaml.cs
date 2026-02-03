using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Mal.DependencyInjection;
using Mdk.Hub.Framework;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     A specialized message box for dangerous operations, requiring the user to type a given key phrase to confirm.
/// </summary>
[Instance]
public partial class DangerBoxView : UserControl
{
    InitialFocusBehavior? _focusBehavior;
    ShakingBehavior? _shakingBehavior;
    DangerBoxViewModel? _trackedDataContext;

    public DangerBoxView()
    {
        InitializeComponent();
        _shakingBehavior = new ShakingBehavior(this);
        _focusBehavior = new InitialFocusBehavior(this);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_trackedDataContext is not null)
            _trackedDataContext.BadVerificationPhrase -= OnBadVerificationPhrase;
        base.OnDataContextChanged(e);
        if (DataContext is DangerBoxViewModel newVm)
        {
            _trackedDataContext = newVm;
            newVm.BadVerificationPhrase += OnBadVerificationPhrase;
        }
    }

    void OnBadVerificationPhrase(object? sender, EventArgs e) => _ = _shakingBehavior?.ShakeAsync() ?? Task.CompletedTask;

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _shakingBehavior?.Dispose();
        _shakingBehavior = null;
        _focusBehavior?.Dispose();
        _focusBehavior = null;
        base.OnUnloaded(e);
    }
}

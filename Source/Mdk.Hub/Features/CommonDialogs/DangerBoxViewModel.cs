using System;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.CommonDialogs;

[ViewModelFor<DangerBoxView>]
public class DangerBoxViewModel : MessageBoxViewModel
{
    string? _keyPhrase;
    public required string? RequiredKeyPhrase { get; init; }
    public required string? KeyPhraseWatermark { get; init; }

    public string? KeyPhrase
    {
        get => _keyPhrase;
        set => SetProperty(ref _keyPhrase, value);
    }

    public event EventHandler? BadVerificationPhrase;

    public bool VerifyKeyPhrase()
    {
        if (!KeyPhrase.EqualsWhileHumanAware(RequiredKeyPhrase))
        {
            BadVerificationPhrase?.Invoke(this, EventArgs.Empty);
            return false;
        }
        return true;
    }
}
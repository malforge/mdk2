using System;
using Mdk.Hub.Framework;
using Mdk.Hub.Utility;

namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
/// View model for a danger confirmation dialog that requires the user to type a specific key phrase for verification.
/// </summary>
[ViewModelFor<DangerBoxView>]
public class DangerBoxViewModel : MessageBoxViewModel
{
    string? _keyPhrase;
    
    /// <summary>
    /// Gets or initializes the key phrase that the user must type to confirm the dangerous action.
    /// </summary>
    public required string? RequiredKeyPhrase { get; init; }
    
    /// <summary>
    /// Gets or initializes the watermark text displayed in the key phrase input field.
    /// </summary>
    public required string? KeyPhraseWatermark { get; init; }

    /// <summary>
    /// Gets or sets the key phrase entered by the user.
    /// </summary>
    public string? KeyPhrase
    {
        get => _keyPhrase;
        set => SetProperty(ref _keyPhrase, value);
    }

    /// <summary>
    /// Occurs when the user enters an incorrect verification phrase.
    /// </summary>
    public event EventHandler? BadVerificationPhrase;

    /// <summary>
    /// Verifies that the entered key phrase matches the required key phrase.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the key phrase matches; otherwise, <see langword="false"/>.
    /// </returns>
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

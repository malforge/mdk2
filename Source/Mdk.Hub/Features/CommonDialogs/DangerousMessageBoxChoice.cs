namespace Mdk.Hub.Features.CommonDialogs;

/// <summary>
///     A message box choice that requires verification before selection, typically for dangerous operations.
/// </summary>
public class DangerousMessageBoxChoice : MessageBoxChoice
{
    /// <summary>
    ///     Called when this choice is selected. Verifies the key phrase before proceeding.
    /// </summary>
    protected override void OnSelected()
    {
        if (ViewModel is not DangerBoxViewModel viewModel)
        {
            base.OnSelected();
            return;
        }
        if (!viewModel.VerifyKeyPhrase())
            return;
        base.OnSelected();
    }
}

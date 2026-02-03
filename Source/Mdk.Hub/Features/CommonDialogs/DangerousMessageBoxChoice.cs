namespace Mdk.Hub.Features.CommonDialogs;

public class DangerousMessageBoxChoice : MessageBoxChoice
{
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

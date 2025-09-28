namespace Mdk.Hub.Features.Shell;

public interface IShell
{
    void Start();
    void AddOverlay(OverlayModel model);
}
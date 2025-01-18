using System.Threading.Tasks;

namespace Mdk.Hub.Services;

public class CommonDialogService: ICommonDialogService
{
    public Task<bool> ConfirmShutdownAsync()
    {
        return Task.FromResult(true);
    }
}
using System.Threading.Tasks;

namespace Mdk.Hub.Services;

public interface ICommonDialogService
{
    Task<bool> ConfirmShutdownAsync();
}
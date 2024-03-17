using System.Net.Http;
using System.Threading.Tasks;

namespace Mdk.CommandLine.SharedApi;

public interface IHttpClient
{
    Task<HttpResponseMessage> GetAsync(string requestUrl);
}
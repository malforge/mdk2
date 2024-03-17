using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

public sealed class WebHttpClient : IHttpClient, IDisposable
{
    HttpClient? _client;

    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }

    public Task<HttpResponseMessage> GetAsync(string requestUrl)
    {
        _client ??= new HttpClient();
        return _client.GetAsync(requestUrl);
    }
}
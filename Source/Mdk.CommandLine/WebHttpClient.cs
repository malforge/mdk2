using System;
using System.Net.Http;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

/// <summary>
///     The default implementation of <see cref="IHttpClient" /> using <see cref="HttpClient" />.
/// </summary>
public sealed class WebHttpClient : IHttpClient, IDisposable
{
    HttpClient? _client;

    /// <inheritdoc />
    public void Dispose()
    {
        _client?.Dispose();
        _client = null;
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> GetAsync(string requestUrl, TimeSpan timeout)
    {
        _client ??= new HttpClient();
        _client.Timeout = timeout;
        return _client.GetAsync(requestUrl);
    }
}
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Mdk.CommandLine.SharedApi;

namespace Mdk.CommandLine;

/// <summary>
///     The default implementation of <see cref="IHttpClient" /> using <see cref="HttpClient" />.
/// </summary>
public sealed class WebHttpClient : IHttpClient, IDisposable
{
    // HttpClient? _client;
    readonly Dictionary<TimeSpan, HttpClient> _clients = new();

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var client in _clients.Values)
            client.Dispose();
        _clients.Clear();
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> GetAsync(string requestUrl, TimeSpan timeout)
    {
        HttpClient? client;
        lock (_clients)
        {
            if (!_clients.TryGetValue(timeout, out client))
            {
                client = new HttpClient();
                client.Timeout = timeout;
                _clients.Add(timeout, client);
            }
        }
        return await client.GetAsync(requestUrl).ConfigureAwait(false);
    }
}
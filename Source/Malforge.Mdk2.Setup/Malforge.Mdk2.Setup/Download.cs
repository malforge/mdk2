using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Malforge.Mdk2.Setup;

public static class Download
{
    private static readonly Lazy<HttpClient> LazyClient = new(() => new HttpClient());

    public static HttpClient Client => LazyClient.Value;

    public static async Task<T?> DownloadJsonAsync<T>(string url, CancellationToken cancellationToken) => await JsonSerializer.DeserializeAsync<T>(await GetStringAsStreamAsync(url), cancellationToken: cancellationToken).ConfigureAwait(false);

    public static async Task<JsonNode?> DownloadJsonAsync(string url, CancellationToken cancellationToken) => await JsonNode.ParseAsync(await GetStringAsStreamAsync(url), cancellationToken: cancellationToken).ConfigureAwait(false);

    private static async Task<Stream> GetStringAsStreamAsync(string url) => await Client.GetStreamAsync(url);
}
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mdk.CommandLine.Shared.Api;

/// <summary>
/// A simple HTTP client interface.
/// </summary>
/// <remarks>
/// The purpose of this interface is to allow for easy mocking of HTTP requests in unit tests.
/// </remarks>
public interface IHttpClient
{
    /// <summary>
    /// Send a GET request to the specified URL.
    /// </summary>
    /// <param name="requestUrl"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<HttpResponseMessage> GetAsync(string requestUrl, TimeSpan timeout);
}
using Common.Config;
using Microsoft.AspNetCore.Http;

namespace Common.HttpClientFactory.Impl;

public class HttpClientWrapper : HttpClientBase, IHttpClientWrapper
{
    private readonly IWebClientConfig _clientsConfig;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpClientWrapper(IHttpContextAccessor httpContextAccessor, IWebClientConfig clientsConfig,
        IHttpClientFactory clientFactory) : base(clientFactory, clientsConfig)
    {
        _httpContextAccessor = httpContextAccessor;
        _clientsConfig = clientsConfig;
    }

    public async Task<TResponse> MakeRequestAsync<TRequest, TResponse>(HttpRequestMessage requestMessage,
        string clientName, TRequest body)
        where TRequest : class
        where TResponse : class
    {
        return await SendAsync<TRequest, TResponse>(requestMessage, clientName, body);
    }
}
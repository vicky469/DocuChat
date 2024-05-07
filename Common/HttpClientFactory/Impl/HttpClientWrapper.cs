using System.Net;
using System.Text.Json;
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

    public async Task<Response<TResponse>> MakeRequestAsync<TRequest, TResponse>(HttpRequestMessage requestMessage,
        string clientName, TRequest body)
        where TRequest : class
        where TResponse : class
    {
        try
        {
            return await SendAsync<TRequest, TResponse>(requestMessage, clientName, body);
        }
        catch (HttpRequestException ex)
        {
            return new Response<TResponse>(false, (HttpStatusCode)ex.StatusCode, null, ex.Message, ex.InnerException);
        }
    }
    
    public async Task<Response<TResponse>> MakeRequestAsync<TResponse>(HttpRequestMessage requestMessage,
        string clientName)
        where TResponse : class
    {
        try
        {
            return await SendAsync<string, TResponse>(requestMessage, clientName);
        }
        catch (HttpRequestException ex)
        {
            return new Response<TResponse>(false, (HttpStatusCode)ex.StatusCode, null, ex.Message, ex.InnerException);
        }
    }
}
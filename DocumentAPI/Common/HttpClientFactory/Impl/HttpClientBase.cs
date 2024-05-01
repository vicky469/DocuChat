using DocumentAPI.Common.Config;
using Newtonsoft.Json;

namespace DocumentAPI.Common.HttpClientFactory.Impl;

public class HttpClientBase : IHttpClient
{
    private readonly IHttpClientFactory _clientFactory;

    public HttpClientBase(IHttpClientFactory clientFactory, IWebClientConfig clientConfig)
    {
        _clientFactory = clientFactory;
    }

    public async Task<TResponse> SendAsync<TRequest, TResponse>(
        HttpRequestMessage requestMessage,
        string clientName = default,
        TRequest body = default,
        JsonSerializerSettings settings = default) where TRequest : class where TResponse : class
    {
        var client = _clientFactory.CreateClient(clientName);
        var request = PreProcess(requestMessage, body, settings);
        var httpResponseMessage = await client.SendAsync(request);
        httpResponseMessage.EnsureSuccessStatusCode();
        return await PostProcess<TResponse>(httpResponseMessage, settings);
    }

    private async Task<TResponse> PostProcess<TResponse>(HttpResponseMessage httpResponseMessage, JsonSerializerSettings settings) where TResponse : class
    {
        var content = await httpResponseMessage.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(content, settings);
    }


    private HttpRequestMessage PreProcess<TRequest>(HttpRequestMessage requestMessage, TRequest body, JsonSerializerSettings settings)
        where TRequest : class
    {
        requestMessage.Content = body != null ? new StringContent(JsonConvert.SerializeObject(body)) : null;
        return requestMessage;
    }
}
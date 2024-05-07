using System.Net;
using Common.Config;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Common.HttpClientFactory.Impl;

public class HttpClientBase : IHttpClient
{
    private readonly IHttpClientFactory _clientFactory;

    public HttpClientBase(IHttpClientFactory clientFactory, IWebClientConfig clientConfig)
    {
        _clientFactory = clientFactory;
    }

    public async Task<Response<TResponse>> SendAsync<TRequest, TResponse>(
        HttpRequestMessage requestMessage,
        string clientName = default,
        TRequest body = default,
        JsonSerializerSettings settings = default) where TRequest : class where TResponse : class
    {
        var client = _clientFactory.CreateClient(clientName);
        var request = PreProcess(requestMessage, body, settings);
        var httpResponse = await client.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();
        return await PostProcess<TResponse>(httpResponse, settings);
    }

    private HttpRequestMessage PreProcess<TRequest>(HttpRequestMessage requestMessage, TRequest body, JsonSerializerSettings settings)
        where TRequest : class
    {
        requestMessage.Content = body != null ? new StringContent(JsonConvert.SerializeObject(body)) : null;
        return requestMessage;
    }
    
    private async Task<Response<TResponse>> PostProcess<TResponse>(HttpResponseMessage httpResponse, JsonSerializerSettings settings) where TResponse : class
    {
        var content = await httpResponse.Content.ReadAsStringAsync();

        if(string.IsNullOrEmpty(content)) return new Response<TResponse>(true, HttpStatusCode.NoContent, null);
            
        if (typeof(TResponse) == typeof(string))
        {
            return new Response<TResponse>(httpResponse.IsSuccessStatusCode, httpResponse.StatusCode,
                content as TResponse);
        }

        if (typeof(TResponse).IsPrimitive)
        {
            var convertedContent = Convert.ChangeType(content, typeof(TResponse));
            return new Response<TResponse>(httpResponse.IsSuccessStatusCode, httpResponse.StatusCode,
                convertedContent as TResponse);
        }

        var deserializedContent = JsonSerializer.Deserialize<TResponse>(content);
        return new Response<TResponse>(httpResponse.IsSuccessStatusCode, httpResponse.StatusCode,
            deserializedContent);
    }
}
using DocumentAPI.Common.Config;
using Newtonsoft.Json;

namespace DocumentAPI.Common.HttpClientFactory.Impl;

public class HttpClientBase : IHttpClient
{
    private readonly IWebClientConfig _clientConfig;
    private readonly IHttpClientFactory _clientFactory;

    public HttpClientBase(IHttpClientFactory clientFactory, IWebClientConfig clientConfig)
    {
        _clientFactory = clientFactory;
        _clientConfig = clientConfig;
    }

    public async Task<HttpClientResponse<TResponse>> SendAsync<TRequest, TResponse>(
        HttpRequestMessage requestMessage,
        string clientName = default,
        TRequest body = default) where TRequest : class where TResponse : class
    {
        HttpClientResponse<TResponse> res = null;
        try
        {
            var client = _clientFactory.CreateClient(clientName);
            var request = PreProcess(requestMessage, body, clientName);
            var httpResponseMessage = await client.SendAsync(request);
            // res = await PostProcess<TResponse>(httpResponseMessage, requestMessage.RequestUri, clientName);
        }
        catch (Exception ex)
        {
            //Logger.Error("Error response for url: {@requestUri}. Reason:{message}. Request:{@requestJson}."),
            //requestMessage.RequestUri, ex.Message, JsonConvert.SerializeObject(requestMessage)));
        }

        return res;
    }


    private HttpRequestMessage PreProcess<TRequest>(HttpRequestMessage requestMessage, TRequest body, string clientName)
        where TRequest : class
    {
        // var clientNameValue = isClientNameHasValue(clientName);
        // var shouldLogRequest = false;
        // _clientConfig[clientName]?.HttpLoggingOptions?.TryGetValue("Request", out shouldLogRequest);
        // if(isClientNameHasValue( && !_clients[clientName].HttpLoggingOptions.IsNullOrEmpty() && shouldLogRequest)
        // {
        //     Logger.Info(Request body: {@body}, JsonConvert.SerializeObject(body));
        // }
        // else{
        requestMessage.Content = body != null ? new StringContent(JsonConvert.SerializeObject(body)) : null;
        // }
        return requestMessage;
    }
}
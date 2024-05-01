using Newtonsoft.Json;

namespace DocumentAPI.Common.HttpClientFactory;

public interface IHttpClient
{
    Task<TResponse> SendAsync<TRequest, TResponse>(HttpRequestMessage requestMessage,
        string clientName = default, TRequest body = default,
        JsonSerializerSettings settings = default)
        where TRequest : class
        where TResponse : class;
}
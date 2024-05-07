using Newtonsoft.Json;

namespace Common.HttpClientFactory;

public interface IHttpClient
{
    Task<Response<TResponse>> SendAsync<TRequest, TResponse>(HttpRequestMessage requestMessage,
        string clientName = default, TRequest body = default,
        JsonSerializerSettings settings = default)
        where TRequest : class
        where TResponse : class;
}
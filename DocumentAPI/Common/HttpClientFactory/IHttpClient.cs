namespace DocumentAPI.Common.HttpClientFactory;

public interface IHttpClient
{
    Task<HttpClientResponse<TResponse>> SendAsync<TRequest, TResponse>(HttpRequestMessage requestMessage,
        string clientName = default, TRequest body = default)
        where TRequest : class
        where TResponse : class;
}
namespace DocumentAPI.Common.HttpClientFactory;

public interface IHttpClientWrapper
{
    Task<TResponse> MakeRequestAsync<TRequest, TResponse>(HttpRequestMessage requestMessage, string clientName,
        TRequest body = default)
        where TRequest : class
        where TResponse : class;
}
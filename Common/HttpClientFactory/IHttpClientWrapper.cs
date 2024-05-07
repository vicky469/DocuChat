namespace Common.HttpClientFactory;

public interface IHttpClientWrapper
{
    Task<Response<TResponse>> MakeRequestAsync<TRequest, TResponse>(HttpRequestMessage requestMessage, string clientName,
        TRequest body)
        where TRequest : class
        where TResponse : class;

    Task<Response<TResponse>> MakeRequestAsync<TResponse>(HttpRequestMessage requestMessage,
        string clientName)
        where TResponse : class;
}

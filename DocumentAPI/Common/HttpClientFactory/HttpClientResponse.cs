using System.Net;

namespace DocumentAPI.Common.HttpClientFactory;

public class HttpClientResponse<TResponse>
{
    public HttpStatusCode StatusCode { get; set; }
    public TResponse Response { get; set; }
    public Exception Exception { get; set; }
}
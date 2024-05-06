namespace DocumentAPI.Service.Integration.SEC;

public interface IRateLimitedHttpClient
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
}
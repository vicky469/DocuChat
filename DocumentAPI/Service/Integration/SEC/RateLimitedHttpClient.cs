namespace DocumentAPI.Service.Integration.SEC;

public class RateLimitedHttpClient: IRateLimitedHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _semaphore;

    public RateLimitedHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // Allow 10 requests at a time
        _semaphore = new SemaphoreSlim(10, 10);
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
    {
        // Wait for a spot to open up in the semaphore
        await _semaphore.WaitAsync();

        try
        {
            // Send the HTTP request
            return await _httpClient.SendAsync(request);
        }
        finally
        {
            // Release the spot in the semaphore after 1 second
            await Task.Delay(1000);
            _semaphore.Release();
        }
    }
}
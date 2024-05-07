namespace Common.HttpClientFactory;

public class RateLimitHttpMessageHandler : DelegatingHandler
{
    private readonly SemaphoreSlim _semaphore;
    private readonly TimeSpan _limitTime;
    private readonly int _limitCount;

    public RateLimitHttpMessageHandler(int limitCount, TimeSpan limitTime)
    {
        
        _limitCount = limitCount;
        _limitTime = limitTime;
        // Allow 10 requests at a time
        _semaphore = new SemaphoreSlim(limitCount, limitCount);
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Wait for a spot to open up in the semaphore
        await _semaphore.WaitAsync();

        try
        {
            // Send the HTTP request
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            // Release the spot in the semaphore after 1 second
            await Task.Delay(_limitTime);
            _semaphore.Release();
        }
    }
}
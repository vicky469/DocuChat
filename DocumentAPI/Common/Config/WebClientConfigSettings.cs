namespace DocumentAPI.Common.Config;

public class WebClientConfigSettings
{
    public Dictionary<string, ClientConfig> WebClientConfigs { get; set; }
}

public class ClientConfig
{
    public string BaseUrl { get; set; }
    public Dictionary<string, string> RequestHeaders { get; set; }
    public int TimeoutInSeconds { get; set; } = 30;
    public int RetryCnt { get; set; } = 0;
    public int BeforeCircuitBreakerCnt { get; set; } = 5;
    public int DurationOfBreakInSeconds { get; set; } = 20;
    public RateLimitConfig RateLimit { get; set; }
    
}
public class RateLimitConfig
{
    public int Requests { get; set; } = 50;
    public int Seconds { get; set; } = 1;
}


public class RateGate : IDisposable
{
    private readonly SemaphoreSlim _semaphore;
    private readonly Timer _timer;

    public RateGate(int occurrences, TimeSpan timeUnit)
    {
        _semaphore = new SemaphoreSlim(occurrences, occurrences);
        _timer = new Timer(x =>
        {
            _semaphore.Release();
        }, null, timeUnit, timeUnit);
    }

    public bool ShouldAllowRequest()
    {
        return _semaphore.Wait(0);
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _semaphore?.Dispose();
    }
}
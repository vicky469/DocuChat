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
}
namespace Common.Config;

public class WebClientConfig : Dictionary<string, ClientConfig>, IWebClientConfig
{
}

public interface IWebClientConfig : IDictionary<string, ClientConfig>
{
}
using DocumentAPI.Common.HttpClientFactory;
using DocumentAPI.Models.Common;
using DocumentAPI.Models.SEC;

namespace DocumentAPI.Services.External;

public class SecClientService: ISecClientService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _secSearchBaseUrl;
    private readonly string _secSearchUrl;
    private static readonly string SEC = "SEC";
    
    public SecClientService(IConfiguration configuration, IHttpClientWrapper httpClientWrapper, IHttpClientFactory clientFactory)
    {
        _configuration = configuration;
        _clientFactory = clientFactory;
        _httpClientWrapper = httpClientWrapper;
        _secSearchBaseUrl = _configuration.GetSection("WebClientConfig:SEC:BaseUrl").Value;
        _secSearchUrl = $"{_secSearchBaseUrl}/{_configuration.GetSection("WebClientConfig:SEC:Endpoint").Value}";
    }
    
    public async Task<string> MakeSecRequest(string url)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        AddSecHeaders(requestMessage);
        var client = _clientFactory.CreateClient();
        var response = await client.SendAsync(requestMessage);
        var content = await response.Content.ReadAsStringAsync();
        return content;
    }

    public async Task<SecSearchResponse> MakeSecSearchRequest(SecFormTypeEnum formType, SecCompanyEnum company, string startDate, string endDate)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get,string.Format(_secSearchUrl, formType, company, startDate, endDate));
        AddSecHeaders(requestMessage);
        var data = await _httpClientWrapper.MakeRequestAsync<string,SecSearchResponse>(requestMessage,SEC);
        return data;
    }
    
    private void AddSecHeaders(HttpRequestMessage requestMessage)
    {
        var userAgent = _configuration[Constants.SecUserAgent];
        requestMessage.Headers.UserAgent.ParseAdd(userAgent);
        requestMessage.Headers.Host = Constants.SecHost;
    }
}
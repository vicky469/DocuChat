using DocumentAPI.Common.Extensions;
using DocumentAPI.Common.HttpClientFactory;
using DocumentAPI.DTO.Common;
using DocumentAPI.DTO.SEC;

namespace DocumentAPI.Service.Integration.SEC;

public class SecClientService : ISecClientService
{
    private static readonly string SEC = "SEC";
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _secSearchBaseUrl;
    private readonly string _secSearchUrl;

    public SecClientService(IConfiguration configuration, IHttpClientWrapper httpClientWrapper,
        IHttpClientFactory clientFactory)
    {
        _configuration = configuration;
        _clientFactory = clientFactory;
        _httpClientWrapper = httpClientWrapper;
        _secSearchBaseUrl = _configuration.GetSection("WebClientConfig:SEC:BaseUrl").Value;
        _secSearchUrl =
            $"{_secSearchBaseUrl}/{_configuration.GetSection("WebClientConfig:SEC:Endpoints:Search").Value}";
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

    public async Task<SecSearchResponse> MakeSecSearchRequest(SecBatchGetUrlsRequestDTO request)
    {
        var uri = string.Format(_secSearchUrl,
            request.CIK,
            request.FormTypeEnum.GetDescription(),
            request.StartDate, request.EndDate);
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        AddSecHeaders(requestMessage);
        requestMessage.Headers.Host = Constants.SecSearchHost;
        var data = await _httpClientWrapper.MakeRequestAsync<string, SecSearchResponse>(requestMessage, SEC);
        return data;
    }

    private void AddSecHeaders(HttpRequestMessage requestMessage)
    {
        var userAgent = _configuration[Constants.SecUserAgent];
        requestMessage.Headers.UserAgent.ParseAdd(userAgent);
        requestMessage.Headers.Host = Constants.SecHost;
    }
}
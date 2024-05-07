using Common.Extensions;
using Common.HttpClientFactory;
using DocumentAPI.DTO.Common;
using DocumentAPI.DTO.SEC;

namespace DocumentAPI.Service.Integration.SEC;

public class SecClientService : ISecClientService
{
    private const string SEC = "SEC";
    private readonly IConfiguration _configuration;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _secSearchBaseUrl;
    private readonly string _secSearchUrl;

    public SecClientService(IConfiguration configuration, IHttpClientWrapper httpClientWrapper)
    {
        _configuration = configuration;
        _httpClientWrapper = httpClientWrapper;
        _secSearchBaseUrl = _configuration.GetSection("WebClientConfig:SEC:BaseUrl").Value;
        _secSearchUrl =
            $"{_secSearchBaseUrl}/{_configuration.GetSection("WebClientConfig:SEC:Endpoints:Search").Value}";
    }

    public async Task<Response<string>> MakeSecRequestAsync(string url)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        AddSecHeaders(requestMessage);
        return await _httpClientWrapper.MakeRequestAsync<string>(requestMessage, SEC);
    }

    public async Task<Response<SecSearchResponse>> MakeSecSearchRequestAsync(SecBatchGetUrlsRequestDTO request)
    {
        var uri = string.Format(_secSearchUrl,
            request.CIK,
            request.FormTypeEnum.GetDescription(),
            request.StartDate, request.EndDate);
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
        AddSecHeaders(requestMessage);
        requestMessage.Headers.Host = Constants.SecSearchHost;
        return await _httpClientWrapper.MakeRequestAsync<SecSearchResponse>(requestMessage, SEC);
    }

    private void AddSecHeaders(HttpRequestMessage requestMessage)
    {
        var userAgent = _configuration[Constants.SecUserAgent];
        requestMessage.Headers.UserAgent.ParseAdd(userAgent);
        requestMessage.Headers.Host = Constants.SecHost;
    }
}
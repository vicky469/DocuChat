using Common.Extensions;
using Common.HttpClientFactory;
using DocumentAPI.DTO.Common;
using DocumentAPI.DTO.SEC;
using System.Text.Json;

namespace DocumentAPI.Service.Integration.SEC;

public class SecClientService : ISecClientService
{
    private static readonly string SEC = "SEC";
    private readonly IHttpClientFactory _clientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientWrapper _httpClientWrapper;
    private readonly string _secSearchBaseUrl;
    private readonly string _secSearchUrl;
    private readonly IRateLimitedHttpClient _rateLimitedHttpClient;

    public SecClientService(IConfiguration configuration, IHttpClientWrapper httpClientWrapper,
        IHttpClientFactory clientFactory, IRateLimitedHttpClient rateLimitedHttpClient)
    {
        _configuration = configuration;
        _clientFactory = clientFactory;
        _httpClientWrapper = httpClientWrapper;
        _secSearchBaseUrl = _configuration.GetSection("WebClientConfig:SEC:BaseUrl").Value;
        _secSearchUrl =
            $"{_secSearchBaseUrl}/{_configuration.GetSection("WebClientConfig:SEC:Endpoints:Search").Value}";
        _rateLimitedHttpClient = rateLimitedHttpClient;
    }

    public async Task<Response<string>> MakeSecRequestAsync(string url)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, url);
        AddSecHeaders(requestMessage);
        var httpResponse = await _rateLimitedHttpClient.SendAsync(requestMessage);
        var content = await httpResponse.Content.ReadAsStringAsync();
        return new Response<string>(httpResponse.IsSuccessStatusCode,httpResponse.StatusCode,content);
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
        //var data = await _httpClientWrapper.MakeRequestAsync<string, SecSearchResponse>(requestMessage, SEC);
        var httpResponse = await _rateLimitedHttpClient.SendAsync(requestMessage);
        var content = await httpResponse.Content.ReadAsStringAsync();
        return new Response<SecSearchResponse>(httpResponse.IsSuccessStatusCode,httpResponse.StatusCode,JsonSerializer.Deserialize<SecSearchResponse>(content));
    }

    private void AddSecHeaders(HttpRequestMessage requestMessage)
    {
        var userAgent = _configuration[Constants.SecUserAgent];
        requestMessage.Headers.UserAgent.ParseAdd(userAgent);
        requestMessage.Headers.Host = Constants.SecHost;
    }
}
using Common.HttpClientFactory;
using DocumentAPI.DTO.SEC;

namespace DocumentAPI.Service.Integration.SEC;

public interface ISecClientService
{
    public Task<Response<string>> MakeSecRequestAsync(string url);

    public Task<Response<SecSearchResponse>> MakeSecSearchRequestAsync(SecBatchGetUrlsRequestDTO request);
}
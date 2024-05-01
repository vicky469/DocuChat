using DocumentAPI.DTO.SEC;

namespace DocumentAPI.Service.Integration.SEC;

public interface ISecClientService
{
    public Task<string> MakeSecRequest(string url);

    public Task<SecSearchResponse> MakeSecSearchRequest(SecBatchGetUrlsRequestDTO request);
}
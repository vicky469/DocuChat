using DocumentAPI.Models.SEC;

namespace DocumentAPI.Services.External;

public interface ISecClientService
{
    public Task<string> MakeSecRequest(string url);

    public Task<SecSearchResponse> MakeSecSearchRequest(SecBatchGetUrlsRequest request);
}
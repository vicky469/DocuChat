using DocumentAPI.Models.SEC;

namespace DocumentAPI.Services.External;

public interface ISecClientService
{
    public Task<string> MakeSecRequest(string url);

    public Task<SecSearchResponse> MakeSecSearchRequest(SecFormTypeEnum formType, SecCompanyEnum company,
        string startDate, string endDate);
}
using DocumentAPI.DTO.SEC;

namespace DocumentAPI.Service;

public interface ISecService
{
    public Task<IResult> ParseDocuments(SecDocumentsParserRequest request);
    public Task<IResult> BatchGetDocumentUrls(SecBatchGetUrlsRequest request);
}
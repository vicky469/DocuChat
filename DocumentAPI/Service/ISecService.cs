using DocumentAPI.DTO.SEC;

namespace DocumentAPI.Service;

public interface ISecService
{
    public Task<IResult> ParseDocuments(SecDocumentsParserRequest request);
    public Task<SecDocumentData> ParseUrlAsync(SecDocumentData data);
    public Task<IResult> BatchGetDocumentUrls(SecBatchGetUrlsRequest request);
}
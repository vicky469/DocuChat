using DocumentAPI.Models;
using DocumentAPI.Models.SEC;

namespace DocumentAPI.Services;

public interface ISecService
{
    public Task<IResult> ParseDocuments(SecDocumentsParserRequest request);
    public Task<IResult> BatchGetDocumentUrls(SecBatchGetUrlsRequest request);
}
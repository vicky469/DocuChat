using Carter;
using Carter.ModelBinding;
using DocumentAPI.DTO.SEC;
using DocumentAPI.Service;
using Microsoft.AspNetCore.Mvc;
using HttpRequest = Microsoft.AspNetCore.Http.HttpRequest;

namespace DocumentAPI.Endpoints;

public class SecModule: CarterModule
{
    public SecModule()
        : base("/api/sec"){
        WithTags("Sec");
        IncludeInOpenApi();        
    }
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/sec-parser", async (HttpRequest req, 
            SecDocumentsParserRequest request, 
            ISecService service,HttpResponse _) =>
        {
            var result = req.Validate<SecDocumentsParserRequest>(request);
            if (!result.IsValid)
            {
                return Results.BadRequest(result.GetFormattedErrors());
            } 
            return await service.ParseDocuments(request);
        });

        app.MapGet("/batch-get-sec-urls", async (ISecService service,
            [FromQuery(Name = "formType")] SecFormTypeEnum formType,
            [FromQuery(Name = "companyList")] string companyList,
            [FromQuery(Name = "startDate")] string startDate,
            [FromQuery(Name = "endDate")] string endDate
        ) =>
        {
            var companies = companyList.Split(',')
                .Select(int.Parse)
                .Select(c => (SecCompanyEnum)c)
                .ToList();
            return await service.BatchGetDocumentUrls(new SecBatchGetUrlsRequest
            {
                FormTypeEnum = formType, CompanyList = companies, StartDate = startDate,
                EndDate = endDate
            });
        });
    }
}
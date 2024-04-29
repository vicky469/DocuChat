using System.Text;
using System.Text.Json;
using DocumentAPI.Common;
using DocumentAPI.Common.Extensions;
using DocumentAPI.Models.Common;
using DocumentAPI.Models.SEC;
using DocumentAPI.Services.External;
using HtmlAgilityPack;

namespace DocumentAPI.Services;

public class SecService: ISecService
{
    private readonly ISecClientService _secClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecService(ISecClientService secClient,IHttpContextAccessor httpContextAccessor)
    {
        _secClient = secClient;
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<IResult> ParseDocuments(SecDocumentsParserRequest request)
    {
        var response = new SecDocumentsParserResponse
        {
            SecDocumentType = request.SecDocumentTypeEnum.GetDescription()
        };
        var data = new List<SecDocumentData>(); 
        var urlChunks = Utils.SplitIntoChunks(request.SecDocumentUrls);
        foreach (var urlChunk in urlChunks)
        {
            var tasks = urlChunk.Select(url => ProcessUrl(request, url, data));
            await Task.WhenAll(tasks);
        }
        
        // Filter the response based on the callingApp
        response.Data = FilterResponse(data);
        response.TotalItems = response.CountTotalItems();
        return Results.Ok(response);
    }

    private List<SecDocumentData> FilterResponse(List<SecDocumentData> data)
    {
        var callingApp = GetCallingApp();
        if(callingApp == null) return data;
        
        if (string.Equals(callingApp, CallingAppEnum.CompanyA.GetDescription(), StringComparison.OrdinalIgnoreCase))
        {
            data = FilterForCompanyA(data);
        }
        return data;
    }

    private string? GetCallingApp()
    {
        string callingApp = null;
        if (_httpContextAccessor.HttpContext.Request.Headers.ContainsKey(Constants.XCallingApp))
        {
            callingApp = _httpContextAccessor.HttpContext.Request.Headers[Constants.XCallingApp].ToString();
        }

        return callingApp;
    }

    public async Task<IResult> BatchGetDocumentUrls(SecBatchGetUrlsRequest request)
    {
        var secUrls = GenerateSecUrls(request);

        // Save to file
        var formType = request.FormTypeEnum.GetDescription();
        var fileName = request.CompanyEnum == 0? 
            $"secUrls_{formType}_{DateTime.Now:yyyyMMddHHmmss}.txt":
            $"secUrls_{request.CompanyEnum.GetDescription()}_{formType}_{DateTime.Now:yyyyMMddHHmmss}.txt";
        
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        await File.WriteAllLinesAsync(filePath, secUrls);
        return Results.Ok(secUrls); 
    }

    private IEnumerable<string> GenerateSecUrls(SecBatchGetUrlsRequest request)
    {
        throw new NotImplementedException();
    }

    private async Task ProcessUrl(SecDocumentsParserRequest request, string url, List<SecDocumentData> responseData)
    {
        var data = new SecDocumentData
        {
            SecDocumentUrl = url
        };
        try
        {
            var htmlDoc = await GetHtmlDoc(url);
            var table = htmlDoc.DocumentNode.SelectSingleNode("//html/body/div[48]/table");
            if (table != null)
            {
                var hrefs = GetAllHrefs(htmlDoc);
                var rows = table.SelectNodes(".//tr");
                data.Items = ParseRows(rows, hrefs, htmlDoc);
                if (data.Items.Any())
                {
                    await SaveJsonToFile(data.Items, url);
                    responseData.Add(data);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse URL: {url}. Error: {ex.Message}");
        }
    }

    private List<Sec10KIndexDTO> ParseRows(HtmlNodeCollection rows, string[] hrefs, HtmlDocument htmlDoc)
    {
        var indexDTO = new List<Sec10KIndexDTO>();
        if (rows != null)
        {
            foreach (var row in rows)
            {
                var cells = row.SelectNodes(".//td");
                if (cells != null)
                {
                    var rowData = new Sec10KIndexDTO();
                    for (var i = 0; i < 2; i++)
                    {
                        var cell = cells[i];
                        if (string.IsNullOrEmpty(cell.InnerText) || cell.InnerText.Trim() == "&#160;") break;
                        switch (i)
                        {
                            case 0 when cell.InnerText.Trim().StartsWith("Item"):
                                rowData.Item = cell.InnerText.Trim();
                                continue;
                            case 1:
                                var name = cell.InnerText.Trim();
                                rowData.ItemNameEnum = EnumEx.TryGetEnumFromDescription<Sec10KFormSectionEnum>(name);
                                rowData.ItemName = name;
                                var nestedLinkNode = cell.SelectSingleNode(".//a");
                                if (nestedLinkNode != null)
                                {
                                    rowData.ItemHref = nestedLinkNode.GetAttributeValue("href", string.Empty);
                                    var index = Array.IndexOf(hrefs, rowData.ItemHref);
                                    string nextHref = null;
                                    if (index >= 0 && index < hrefs.Length - 1)
                                    {
                                        nextHref = hrefs[index + 1];
                                    }
                                    var content = GetContentBetweenIds(htmlDoc, rowData.ItemHref,nextHref);
                                    if (!string.IsNullOrEmpty(content))
                                    {
                                        rowData.ItemValue = content;
                                    }
                                }
                                continue;
                        }
                    }

                    if (rowData.Item != null && rowData.ItemName != null)
                    {
                        indexDTO.Add(rowData);   
                    }
                }
            }
        }
        return indexDTO;
    }

    private async Task<HtmlDocument> GetHtmlDoc(string url)
    {
        var htmlDoc = new HtmlDocument();
        var fileName = Path.GetFileName(url);
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Raw");
        Directory.CreateDirectory(dataDirectory); // Ensure the directory exists
        var filePath = Path.Combine(dataDirectory, fileName);
        string content;
        if (!File.Exists(filePath))
        {
            content = await _secClient.MakeSecRequest(url);
            await File.WriteAllTextAsync(filePath, content);
        }
        else
        {
            content = await File.ReadAllTextAsync(filePath);
        }
        htmlDoc.LoadHtml(content);
        return htmlDoc;
    }
    
    private static async Task SaveJsonToFile(List<Sec10KIndexDTO> indexDTO, string url)
    {
        var fileName = Path.GetFileName(url);
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Result");
        Directory.CreateDirectory(dataDirectory); // Ensure the directory exists
        var filePath = Path.Combine(dataDirectory, Path.ChangeExtension(fileName, ".json"));
        var jsonData = JsonSerializer.Serialize(indexDTO);
        await File.WriteAllTextAsync(filePath, jsonData);
    }
    
    
    private static string GetContentBetweenIds(HtmlDocument htmlDoc, string startId, string endId)
    {
        // Remove the '#' from the start of the ids if it's there
        if (startId.StartsWith("#"))
        {
            startId = startId.Substring(1);
        }
        if (endId.StartsWith("#"))
        {
            endId = endId.Substring(1);
        }

        // Use XPath to find the elements with the specific ids
        var xPathStart = $"//*[@id='{startId}']";
        var xPathEnd = $"//*[@id='{endId}']";
        var startNode = htmlDoc.DocumentNode.SelectSingleNode(xPathStart);
        var endNode = htmlDoc.DocumentNode.SelectSingleNode(xPathEnd);

        if (startNode == null || endNode == null)
        {
            return null;
        }

        var content = new StringBuilder();
        var currentNode = startNode;
        while (currentNode != endNode)
        {
            content.AppendLine(currentNode.InnerHtml);
            currentNode = currentNode.NextSibling;
            if (currentNode == null)
            {
                break;
            }
        }

        return content.ToString();
    }
    
    private static string[] GetAllHrefs(HtmlDocument htmlDoc)
    {
        // Use XPath to find all 'a' elements
        var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a");

        if (linkNodes == null)
        {
            return new string[0];
        }

        // Extract the 'href' attribute from each 'a' element
        var hrefs = linkNodes
            .Select(node => node.GetAttributeValue("href", string.Empty))
            .Where(href => !string.IsNullOrEmpty(href))
            .GroupBy(href => href)
            .SelectMany(group => group.Skip(1))
            .ToArray();

        return hrefs;
    }
    
    private List<SecDocumentData> FilterForCompanyA(List<SecDocumentData> data)
    {
        var sectionsToInclude = new List<Sec10KFormSectionEnum>
        {
            Sec10KFormSectionEnum.Sec1,    // Business
            Sec10KFormSectionEnum.Sec1A,   // Risk Factors
            Sec10KFormSectionEnum.Sec2,    // Properties
            Sec10KFormSectionEnum.Sec3,    // Legal Proceedings
            Sec10KFormSectionEnum.Sec7,    // Management’s Discussion and Analysis of Financial Condition and Results of Operations
            Sec10KFormSectionEnum.Sec7A,   // Quantitative and Qualitative Disclosures about Market Risk
            Sec10KFormSectionEnum.Sec9A    // Controls and Procedures
        };

        foreach (var document in data)
        {
            document.Items = document.Items
                .Where(item => item.ItemNameEnum.HasValue && sectionsToInclude.Contains((Sec10KFormSectionEnum)item.ItemNameEnum))
                .ToList();
        }

        return data;
    }
}
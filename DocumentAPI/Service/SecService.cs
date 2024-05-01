using System.Text;
using AutoMapper;
using DocumentAPI.Common;
using DocumentAPI.Common.Extensions;
using DocumentAPI.DTO.Common;
using DocumentAPI.DTO.SEC;
using DocumentAPI.Infrastructure.Entity;
using DocumentAPI.Infrastructure.Repository;
using DocumentAPI.Service.Integration.SEC;
using HtmlAgilityPack;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DocumentAPI.Service;

public class SecService : ISecService
{
    private static readonly List<string> IndexKeywords = new()
    {
        "INDEX",
        "TABLE OF CONTENTS"
    };

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISecClientService _secClient;
    private readonly IFileRepository _fileRepository;
    private readonly IMapper _mapper;

    public SecService(ISecClientService secClient, IHttpContextAccessor httpContextAccessor, IFileRepository fileRepository, IMapper mapper)
    {
        _secClient = secClient;
        _httpContextAccessor = httpContextAccessor;
        _fileRepository = fileRepository;
        _mapper = mapper;
    }

    public async Task<IResult> ParseDocuments(SecDocumentsParserRequest request)
    {
        var response = new SecDocumentsParserResponse
        {
            SecDocumentType = request.SecDocumentTypeEnum.GetDescription(),
            Data = new List<SecDocumentData>()
        };
        var urlChunks = Utils.SplitIntoChunks(request.SecDocumentUrls);
        foreach (var urlChunk in urlChunks)
        {
            var tasks = urlChunk.Select(url =>
            {
                var data = new SecDocumentData();
                data.SecDocumentUrl = url;
                return ProcessUrl(data);
            });
            var results = await Task.WhenAll(tasks);
            response.Data.AddRange(results);
        }

        // Filter the response based on the callingApp
        response.Data = await FilterResponse(response.Data);
        response.TotalItems = response.CountTotalItems();
        response.Data.ForEach(d => d.ItemsCnt = d.Items.Count);
        return Results.Ok(response);
    }

    public async Task<IResult> BatchGetDocumentUrls(SecBatchGetUrlsRequest request)
    {
        var finalUrls = new List<string>();
        foreach (var company in request.CompanyList)
        {
            var secUrls = await GetSecUrls(request, company);
            var finalUrlsForCompany = new List<string>();
            foreach (var url in secUrls)
            {
                var parts = url.Split(':', '-');
                var first = parts[0].TrimStart('0');
                var lastStartIndex = url.IndexOf(parts[3]);
                var last = url.Substring(lastStartIndex);
                var finalUrl = $"https://www.sec.gov/Archives/edgar/data/{first}/{parts[0]}{parts[1]}{parts[2]}/{last}";
                finalUrlsForCompany.Add(finalUrl);
            }

            // Save to file
            var formType = request.FormTypeEnum.GetDescription();
            var fileName = $"secUrls_{company.GetDescription()}_{formType}_{DateTime.Now:yyyyMMddHHmmss}.txt";
            var fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), FileRepository.BatchUrlDirectory);
            await _fileRepository.SaveToFileAsync(fileDirectory,fileName, finalUrlsForCompany);
            finalUrls.AddRange(finalUrlsForCompany);
        }

        return Results.Ok(finalUrls);
    }

    #region private methods
    private async Task<List<SecDocumentData>> FilterResponse(List<SecDocumentData> data)
    {
        var callingApp = GetCallingApp();
        if (callingApp == null) return data;

        if (string.Equals(callingApp, CallingAppEnum.CompanyA.GetDescription(), StringComparison.OrdinalIgnoreCase))
            data = await FilterForCompanyA(data);
        return data;
    }

    private string? GetCallingApp() => _httpContextAccessor?.HttpContext?.Request?.Headers[Constants.XCallingApp].ToString();

    private async Task<List<string>> GetSecUrls(SecBatchGetUrlsRequest request, SecCompanyEnum currnetCompany)
    {
        var companies = await CIKLookup();
        var secRequest = new SecBatchGetUrlsRequestDTO();
        var company = companies.FirstOrDefault(c =>
            string.Equals(c.Title, currnetCompany.GetDescription(), StringComparison.OrdinalIgnoreCase));
        if (company != null)
        {
            secRequest.CIK = company.CIK_Str_Padded;
            secRequest.FormTypeEnum = request.FormTypeEnum;
            secRequest.StartDate = request.StartDate;
            secRequest.EndDate = request.EndDate;
        }

        var data = await _secClient.MakeSecSearchRequest(secRequest);
        var idList = data?.hits.hits.Select(hit => hit._id).ToList();
        return idList;
    }
    
    private async Task<List<CompanyDTO>> CIKLookup()
    {
        List<CompanyDTO> companies;
        var fileDirectory = FileRepository.PersistentDataDirectory;
        var existingFiles = _fileRepository.IsFileExist(fileDirectory,"transformed_company_tickers*");
        if (!existingFiles.Any())
        {
            var filePath = Path.Combine(fileDirectory, "company_tickers.json");
            var jsonData = await File.ReadAllTextAsync(filePath);
            var companyEntities = JsonConvert.DeserializeObject<Dictionary<string, CompanyEntity>>(jsonData);
            companies = _mapper.Map<List<CompanyEntity>, List<CompanyDTO>>(companyEntities.Values.ToList());
            await _fileRepository.SaveToFileAsync(fileDirectory,$"transformed_company_tickers_{DateTime.Now:yyyyMMddHHmmss}.json", companies);
        }
        else
        {
            var filePath = existingFiles.First();
            var jsonData = await File.ReadAllTextAsync(filePath);
            var companyEntities = JsonConvert.DeserializeObject<Dictionary<string, CompanyEntity>>(jsonData);
            companies = _mapper.Map<List<CompanyEntity>, List<CompanyDTO>>(companyEntities.Values.ToList());
        }

        return companies;
    }

    private async Task<SecDocumentData> ProcessUrl(SecDocumentData data)
    {
        try
        {
            var htmlDoc = await GetHtmlDoc(data.SecDocumentUrl);
            var divNodes = htmlDoc.DocumentNode.SelectNodes("//html/body/div");
            if (divNodes == null)
            {
                Console.WriteLine($"Failed to parse URL: {data.SecDocumentUrl}. No div nodes found.");
                return data;
            }

            var table = GetHtmlTable(divNodes, htmlDoc);
            if (table == null)
            {
                Console.WriteLine($"Failed to parse URL: {data.SecDocumentUrl}. No div nodes found.");
                return data;
            }

            var hrefs = GetAllHrefs(htmlDoc);
            var rows = table.SelectNodes(".//tr");
            if (rows == null)
            {
                Console.WriteLine($"Failed to parse URL: {data.SecDocumentUrl}. No rows found.");
                return data;
            }

            data.Items = GetItemsFromRows(rows, hrefs, htmlDoc);
            if (data.Items.Count == 0) Console.WriteLine($"Failed to URL: {data.SecDocumentUrl}. No items found.");
            await SaveJsonToFile(data.Items, data.SecDocumentUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse URL: {data.SecDocumentUrl}. Error: {ex.Message}");
        }

        return data;
    }

    private static HtmlNode? GetHtmlTable(HtmlNodeCollection divNodes, HtmlDocument htmlDoc)
    {
        var divIndex = 0;
        for (var i = 0; i < divNodes.Count; i++)
            if (divNodes[i].InnerHtml.Contains("INDEX") || divNodes[i].InnerHtml.Contains("TABLE OF CONTENTS"))
            {
                divIndex = i + 1; // XPath is 1-indexed 
                while (divIndex < divNodes.Count && string.IsNullOrWhiteSpace(divNodes[divIndex].InnerText)) divIndex++;
                break;
            }

        HtmlNode table = null;
        if (divIndex > 0)
        {
            var tableXPath = $"//html/body/div[{divIndex + 1}]/table";
            table = htmlDoc.DocumentNode.SelectSingleNode(tableXPath);
        }

        return table;
    }

    private List<Sec10KIndexDTO> GetItemsFromRows(HtmlNodeCollection rows, string[] hrefs, HtmlDocument htmlDoc)
    {
        var items = new List<Sec10KIndexDTO>();

        foreach (var row in rows)
        {
            var cols = row.SelectNodes(".//td");
            if (cols != null)
            {
                var rowData = new Sec10KIndexDTO();
                var cellValues = cols.Select(cell => cell.InnerText.Trim()).ToList();
                var containsItem = cellValues.Any(value => value.Contains("Item"));
                if (containsItem)
                {
                    rowData.Item = cols[0].InnerText;
                    rowData.ItemName = cols[1].InnerText;
                    rowData.ItemNameEnum = EnumEx.TryGetEnumFromDescription<Sec10KFormSectionEnum>(rowData.ItemName);
                    var nestedLinkNode = cols[1].SelectSingleNode(".//a");
                    if (nestedLinkNode != null)
                    {
                        rowData.ItemHref = nestedLinkNode.GetAttributeValue("href", string.Empty);
                        var index = Array.IndexOf(hrefs, rowData.ItemHref);
                        string nextHref = null;
                        if (index >= 0 && index < hrefs.Length - 1) nextHref = hrefs[index + 1];
                        var content = GetContentBetweenIds(htmlDoc, rowData.ItemHref, nextHref);
                        if (!string.IsNullOrEmpty(content)) rowData.ItemValue = content;
                    }

                    if (rowData.Item != null && rowData.ItemName != null) items.Add(rowData);
                }
            }
        }

        return items;
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
        if (startId.StartsWith("#")) startId = startId.Substring(1);
        if (endId.StartsWith("#")) endId = endId.Substring(1);

        // Use XPath to find the elements with the specific ids
        var xPathStart = $"//*[@id='{startId}']";
        var xPathEnd = $"//*[@id='{endId}']";
        var startNode = htmlDoc.DocumentNode.SelectSingleNode(xPathStart);
        var endNode = htmlDoc.DocumentNode.SelectSingleNode(xPathEnd);

        while (startNode != null && string.IsNullOrWhiteSpace(startNode.InnerText))
            startNode = startNode.SelectSingleNode("following-sibling::div[not(@id)]");
        while (endNode != null && string.IsNullOrWhiteSpace(endNode.InnerText))
            endNode = endNode.SelectSingleNode("following-sibling::div[not(@id)]");
        if (startNode == null || endNode == null) return string.Empty;
        var content = new StringBuilder();
        var currentNode = startNode;
        while (currentNode != endNode)
        {
            content.AppendLine(currentNode.InnerHtml);
            currentNode = currentNode.NextSibling;
            if (currentNode == null) break;
        }

        return content.ToString();
    }

    private static string[] GetAllHrefs(HtmlDocument htmlDoc)
    {
        // Use XPath to find all 'a' elements
        var linkNodes = htmlDoc.DocumentNode.SelectNodes("//a");

        if (linkNodes == null) return new string[0];

        // Extract the 'href' attribute from each 'a' element
        var hrefs = linkNodes
            .Select(node => node.GetAttributeValue("href", string.Empty))
            .Where(href => !string.IsNullOrEmpty(href))
            .GroupBy(href => href)
            .SelectMany(group => group.Skip(1))
            .Distinct()
            .ToArray();

        return hrefs;
    }

    private async Task<List<SecDocumentData>> FilterForCompanyA(List<SecDocumentData> data)
    {
        var itemsToInclude = new List<Sec10KFormSectionEnum>
        {
            Sec10KFormSectionEnum.Item1, // Business
            Sec10KFormSectionEnum.Item1A, // Risk Factors
            Sec10KFormSectionEnum.Item2, // Properties
            Sec10KFormSectionEnum.Item3, // Legal Proceedings
            Sec10KFormSectionEnum
                .Item7, // Management’s Discussion and Analysis of Financial Condition and Results of Operations
            Sec10KFormSectionEnum.Item7A, // Quantitative and Qualitative Disclosures about Market Risk
            Sec10KFormSectionEnum.Item9A // Controls and Procedures
        };

        foreach (var document in data)
            try
            {
                document.Items = document.Items
                    .Where(item =>
                        item.ItemNameEnum.HasValue && itemsToInclude.Contains((Sec10KFormSectionEnum)item.ItemNameEnum))
                    .ToList();

                // audit the items to ensure all required items are present
                var itemNames = document.Items.Where(item => item.ItemNameEnum.HasValue)
                    .Select(item => item.ItemNameEnum.Value)
                    .ToList();
                var missingItems = itemsToInclude.Except(itemNames).ToList();

                if (missingItems.Any())
                    throw new Exception(
                        $"url {document.SecDocumentUrl} is missing item: {string.Join(", ", missingItems)}");
            }
            catch (Exception e)
            {
                var failedDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data/Failed");
                Directory.CreateDirectory(failedDirectory);
                var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.txt";
                var filePath = Path.Combine(failedDirectory, fileName);
                await Task.Run(() => File.WriteAllText(filePath, document.SecDocumentUrl));

                throw;
            }

        return data;
    }
    #endregion
}
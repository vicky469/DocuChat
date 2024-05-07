using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using AutoMapper;
using Common;
using Common.Extensions;
using Common.HttpClientFactory;
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
    private static HashSet<string> ItemsToInclude;
    private static readonly HashSet<string> IndexKeywords =
    [
        "INDEX",
        "TABLE OF CONTENTS"
    ];

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
        var callingApp = GetCallingApp();
        if (string.Equals(callingApp, CallingAppEnum.CompanyA.GetDescription(), StringComparison.OrdinalIgnoreCase))
        {
            ItemsToInclude =
            [
                "Item1", // Business
                "Item1A", // Risk Factors
                "Item2", // Properties
                "Item3", // Legal Proceedings
                "Item7", // Management’s Discussion and Analysis of Financial Condition and Results of Operations
                "Item7A", // Quantitative and Qualitative Disclosures about Market Risk
                "Item9A"
            ];
        }
        else
        {
            ItemsToInclude = new HashSet<string>(Enum.GetValues(typeof(Sec10KFormSectionEnum))
                .Cast<Sec10KFormSectionEnum>()
                .Select(item => item.ToString() + "."));
        }
    }

    public async Task<IResult> ParseDocuments(SecDocumentsParserRequest request)
    {
        var response = new SecDocumentsParserResponse
        {
            SecDocumentType = request.SecDocumentTypeEnum.GetDescription(),
            RequestedUrls = request.SecDocumentUrls.Length,
            Sections = []
        };
        var urlChunks = Utils.SplitIntoChunks(request.SecDocumentUrls);
        for (var i = 0; i < urlChunks.Count; i++)
        {
            var tasks = urlChunks[i].Select(url =>
            {
                var targetItems = (request.TargetItemsInDocument?.Any() == true ? request.TargetItemsInDocument : ItemsToInclude);
                var data = new SecDocumentData { SecDocumentUrl = url , TargetItemsInDocument = targetItems};
                return ParseUrlAsync(data);
            });
            var results = await Task.WhenAll(tasks);
            response.Sections.AddRange(results);
        }
        response.TotalItems = response.CountTotalItems();
        response.CountTotalItemsPerSection();
        response.FailedUrls = response.CountTotalFailedUrls();
        return Results.Ok(response);
    }
    
    public async Task<SecDocumentData> ParseUrlAsync(SecDocumentData data)
    {
        Console.WriteLine($"Parsing URL: {data.SecDocumentUrl}");
        try
        {
            var htmlDoc = await GetHtmlDocAsync(data.SecDocumentUrl);
            var table = GetHtmlTable(htmlDoc);
            if (table == null) throw new Exception($"Failed to parse URL: {data.SecDocumentUrl}. No table found.");
            
            var hrefs = GetAllHrefs(table);
            var rows = table.SelectNodes(".//tr");
            if (rows == null)throw new Exception($"Failed to parse URL: {data.SecDocumentUrl}. No rows found.");
            
            data.Items = ParseRowsParallel(rows, hrefs, htmlDoc,data.TargetItemsInDocument);
            if (data.Items == null || data.Items.Count == 0) throw new Exception($"Failed to URL: {data.SecDocumentUrl}. No items found.");
            data.Items = data.Items.OrderBy(item => item.ItemNameEnum).ToList();
            await SaveResultToFile(data.Items, data.SecDocumentUrl);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            var failedDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data/Failed");
            Directory.CreateDirectory(failedDirectory);
            var fileName = $"{DateTime.Now:yyyyMMddHHmmss}.txt";
            var filePath = Path.Combine(failedDirectory, fileName);
            await File.WriteAllTextAsync(filePath, data.SecDocumentUrl);
        }
        Console.WriteLine($"Finished parsing URL: {data.SecDocumentUrl}");
        return data;
    }

    public async Task<IResult> BatchGetDocumentUrls(SecBatchGetUrlsRequest request)
    {
        var finalUrls = new List<string>();
        var companies = await CIKLookup(request.Size);
        
        if(request?.CompanyList == null || request.CompanyList.Count == 0)
        {
            foreach (var company in companies)
            {
                var companyDTO = companies.FirstOrDefault(c =>
                    string.Equals(c.Ticker, company.CompanyEnum.GetDescription(), StringComparison.OrdinalIgnoreCase));
                finalUrls.AddRange(await ProcessAndSaveUrlsAsync(request, companyDTO));
            }
        }

        if (finalUrls.Count == 0) return Results.Problem("No URLs found for the specified companies.");
        return Results.Ok(finalUrls);
    }

    #region private methods
    private async Task<List<string>> ProcessAndSaveUrlsAsync(SecBatchGetUrlsRequest request, CompanyDTO companyDTO)
    {
        var finalUrlsForCompany = new List<string>();
        var secUrls = await GetSecUrlsAsync(request, companyDTO);
        
        foreach (var url in secUrls)
        {
            var parts = url.Split(':', '-');
            var cik = companyDTO.CIK_Str;
            var lastStartIndex = url.IndexOf(parts[3]);
            var last = url.Substring(lastStartIndex);
            var finalUrl = $"https://www.sec.gov/Archives/edgar/data/{cik}/{parts[0]}{parts[1]}{parts[2]}/{last}";
            finalUrlsForCompany.Add(finalUrl);
        }

        // Save to file
        if (secUrls.Count > 0)
        {
            var formType = request.FormTypeEnum.GetDescription();
            var fileName = $"secUrls_{companyDTO.CompanyEnum.GetDescription()}_{formType}_{DateTime.Now:yyyyMMddHHmmss}.txt";
            var fileDirectory = Path.Combine(Directory.GetCurrentDirectory(), FileRepository.BatchUrlDirectory);
            await _fileRepository.SaveToFileAsync(fileDirectory,fileName, finalUrlsForCompany);
        }

        return finalUrlsForCompany;
    }

    private string? GetCallingApp() => _httpContextAccessor?.HttpContext?.Request?.Headers[Constants.XCallingApp].ToString();

    private async Task<List<string>> GetSecUrlsAsync(SecBatchGetUrlsRequest request, CompanyDTO companyDTO)
    {
        var idList = new List<string>();
        var secRequest = new SecBatchGetUrlsRequestDTO();
        if (companyDTO != null)
        {
            secRequest.CIK = companyDTO.CIK_Str_Padded;
            secRequest.FormTypeEnum = request.FormTypeEnum;
            secRequest.StartDate = request.StartDate;
            secRequest.EndDate = request.EndDate;
        }

        var response = await _secClient.MakeSecSearchRequestAsync(secRequest);
        if (!response.IsSuccess) return idList;
        return response?.Data?.hits.hits.Select(hit => hit._id).ToList();
    }
    
    private async Task<List<CompanyDTO>> CIKLookup(int size)
    {
        List<CompanyDTO> companies;
        var fileDirectory = FileRepository.PersistentDataDirectory;
        var existingFiles = _fileRepository.IsFileExist(fileDirectory,"transformed_company_tickers*");
        if (!existingFiles.Any())
        {
            var filePath = Path.Combine(fileDirectory, "company_tickers.json");
            var jsonData = await File.ReadAllTextAsync(filePath);
            var companyEntities = JsonConvert.DeserializeObject<Dictionary<string, CompanyEntity>>(jsonData).Values.Take(50).ToList();
            //await BuildCompanyEnumAsync(companyEntities, fileDirectory);
            companies = _mapper.Map<List<CompanyEntity>, List<CompanyDTO>>(companyEntities);
            await _fileRepository.SaveToFileAsync(fileDirectory,$"transformed_company_tickers_{DateTime.Now:yyyyMMddHHmmss}.json", companies);
        }
        else
        {
            var filePath = existingFiles.First();
            var jsonData = await File.ReadAllTextAsync(filePath);
            companies = JsonConvert.DeserializeObject<List<CompanyDTO>>(jsonData);
        }
        return companies;
    }

    private static HtmlNode GetHtmlTable(HtmlDocument htmlDoc)
    {
        HtmlNode tableNode = null;
        var divNodes = htmlDoc.DocumentNode.SelectNodes("//html/body/div");
        // 1st try
        foreach (var keyword in IndexKeywords)
        {
            var keywordNode = htmlDoc.DocumentNode.SelectSingleNode($"//*[text()[contains(., '{keyword}')]]");
        
            if (keywordNode != null)
            {
                for (int i = 1; i <= 5; i++)
                {
                    var potentialNode = keywordNode.SelectSingleNode($".//following-sibling::node()[{i}]");
                    if (potentialNode?.InnerText != null && potentialNode.InnerText.Contains("PART"))
                    {
                        return potentialNode.SelectSingleNode(".//following-sibling::table");
                    }
                }
            }
        }
        
        // 2nd try
        var divIndex = 0;
        for (var i = 0; i < divNodes.Count; i++)
        {
            if (IndexKeywords.Any(keyword => divNodes[i].InnerHtml.IndexOf(keyword) >= 0))
            {
                divIndex = i + 1; // XPath is 1-indexed 
                while (divIndex < divNodes.Count && string.IsNullOrWhiteSpace(divNodes[divIndex].InnerText))
                    divIndex++;
                break;
            }
        }
        
        if (divIndex > 0)
        {
            var tableXPath = $"//html/body/div[{divIndex + 1}]/table";
            tableNode = htmlDoc.DocumentNode.SelectSingleNode(tableXPath);
            if (tableNode == null)
            {
                tableXPath = $"//html/body/div[{divIndex + 1}]/div/table";
                tableNode = htmlDoc.DocumentNode.SelectSingleNode(tableXPath);
            }
        }
        

        return tableNode;
    }
    
    private List<Sec10KIndexDTO> ParseRowsParallel(HtmlNodeCollection rows, string[] hrefs, HtmlDocument htmlDoc, HashSet<string> targetItemsInDocument)
    {
        Console.WriteLine($"Parsing {rows.Count} rows.");
        var items = new ConcurrentBag<Sec10KIndexDTO>();

        Parallel.ForEach(rows.Cast<HtmlNode>(), row =>
        {
            var cols = row.SelectNodes(".//td");
            if (cols != null)
            {
                var cellValues = cols.Select(cell =>
                        HtmlEntity.DeEntitize(cell.InnerText.Trim()))
                    .Select(val => Regex.Replace(val, @"[^\u0000-\u007F\u2019]+", string.Empty)) // Allow Unicode apostrophe
                    .Select(val => Regex.Replace(val, @"\n|&#160;", " "))
                    .Where(val => !string.IsNullOrEmpty(val))
                    .ToList();
                var containsItem = cellValues.Any(value => value.Contains("Item"));
                if (containsItem)
                {
                    var sectionData = new Sec10KIndexDTO
                    {
                        Item = cellValues[0],
                        ItemName = cellValues[1]
                    };
                    sectionData.ItemNameEnum = EnumEx.TryGetEnumFromDescription<Sec10KFormSectionEnum>(sectionData.ItemName);
                    if (sectionData.ItemNameEnum == null)Console.WriteLine("Failed to parse item name.");
                    if (targetItemsInDocument.Any(item => item.Equals(sectionData.Item.Replace(" ", "").TrimEnd('.'), StringComparison.OrdinalIgnoreCase))) 
                        AddSectionDetail(hrefs, htmlDoc, cols, sectionData, row, items);
                }
            }
        });

        Console.WriteLine($"Finished parsing rows, {items.Count} items.");
        return items.ToList();
    }

    private void AddSectionDetail(string[] hrefs, HtmlDocument htmlDoc, HtmlNodeCollection cols, Sec10KIndexDTO sectionData,
        HtmlNode row, ConcurrentBag<Sec10KIndexDTO> items)
    {
        var itemNameNode = cols.FirstOrDefault(cell =>
        {
            var cleanedText = HtmlEntity.DeEntitize(cell.InnerText.Trim());
            cleanedText = Regex.Replace(cleanedText, @"\n|&#160;", string.Empty);
            return cleanedText.Contains(sectionData.ItemName);
        });
        if (itemNameNode == null) throw new Exception($"Failed to get itemNameNode for {row}.");
        var nestedLinkNode = itemNameNode.SelectSingleNode(".//a");
        if (nestedLinkNode != null)
        {
            sectionData.ItemHref = nestedLinkNode.GetAttributeValue("href", string.Empty);
            var index = Array.IndexOf(hrefs, sectionData.ItemHref);
            if (index == -1) throw new Exception("Failed to find href in list of hrefs.");
            string nextHref = null;
            if (index >= 0 && index < hrefs.Length - 1) nextHref = hrefs[index + 1];
            // Remove the '#' from the start of the ids if it's there
            if (sectionData.ItemHref.StartsWith("#"))
                sectionData.ItemHref = sectionData.ItemHref.Substring(1);
            if (nextHref.StartsWith("#")) nextHref = nextHref.Substring(1);

            Console.WriteLine($"Parsing section {sectionData.Item} with href {sectionData.ItemHref} and nextHref {nextHref}.");
            var sections = GetHtmlSectionById(htmlDoc, sectionData.ItemHref, nextHref);
            Console.WriteLine($"Finished parsing section {sectionData.Item} with href {sectionData.ItemHref} and nextHref {nextHref}.");

            if (sections?.Count > 0) sectionData.ItemValue = sections;
        }
        items.Add(sectionData);
    }

    private async Task<HtmlDocument> GetHtmlDocAsync(string url)
    {
        var htmlDoc = new HtmlDocument();
        var fileName = Path.GetFileName(url);
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Raw");
        Directory.CreateDirectory(dataDirectory); // Ensure the directory exists
        var filePath = Path.Combine(dataDirectory, fileName);
        Response<string> response = new Response<string>();
        if (!File.Exists(filePath))
        {
            response = await _secClient.MakeSecRequestAsync(url);
            if (response.IsSuccess)
            {
                await File.WriteAllTextAsync(filePath, response.Data);
            }
        }
        else
        {
            response.Data = await File.ReadAllTextAsync(filePath);
        }

        htmlDoc.LoadHtml(response.Data);
        return htmlDoc;
    }

    private static async Task SaveResultToFile(List<Sec10KIndexDTO> indexDTO, string url)
    {
        var fileName = Path.GetFileName(url);
        var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Result");
        Directory.CreateDirectory(dataDirectory); // Ensure the directory exists
        var filePath = Path.Combine(dataDirectory, Path.ChangeExtension(fileName, ".json"));
        var jsonData = JsonSerializer.Serialize(indexDTO);
        await File.WriteAllTextAsync(filePath, jsonData);
    }

    private Dictionary<string, string> GetHtmlSectionById(HtmlDocument htmlDoc, string startId, string endId)
    {
        var sections = new ConcurrentDictionary<string, StringBuilder>();

        // Use XPath to find the elements with the specific ids
        var xPathStart = $"//*[@id='{startId}']";
        var xPathEnd = $"//*[@id='{endId}']";
        var startNode = htmlDoc.DocumentNode.SelectSingleNode(xPathStart);
        var endNode = htmlDoc.DocumentNode.SelectSingleNode(xPathEnd);

        var currentNode = startNode;
        var dicKey = string.Empty;
        
        // Loop through the siblings of the start node until we reach the end node
        while (currentNode != endNode)
        {
            if (currentNode == null || currentNode.InnerHtml.Contains(endId)) break;
            if (currentNode.InnerHtml.Contains("<table"))
            {
                // Parse table
                var tableNode = currentNode.SelectSingleNode(".//table");
                if (tableNode != null)
                {
                    var rows = currentNode.SelectNodes(".//tr");
                    var keys = new List<string>();
                    foreach (var row in rows)
                    {
                        var cells = row.SelectNodes(".//td")
                            .Where(cell => !string.IsNullOrEmpty(HtmlEntity.DeEntitize(cell.InnerText.Trim())))
                            .ToList();
                        for (var i = 0; i < cells.Count; i++)
                        {
                            var cell = HtmlEntity.DeEntitize(cells[i].InnerText.Trim());
                            if (keys.Count < cells.Count)
                            {
                                // create dictionary key
                                keys.Add(cell);
                                sections[cell] = new StringBuilder();
                            }
                            else if (sections.TryGetValue(keys[i], out var existingVal))
                            {
                                existingVal.Append(cell + ". ");
                                sections[keys[i]] =existingVal;
                            }
                        }
                    }
                }
            }
            else
            {
                // Parse Text
                var innerText = currentNode.InnerText;
                var cleanedText = HtmlEntity.DeEntitize(innerText.Trim());
                var textBeforeDot = cleanedText.Split('.')[0].Trim();
                if (ItemsToInclude.Any(item => item.Equals(textBeforeDot, StringComparison.OrdinalIgnoreCase)))
                    continue;
                if (string.IsNullOrEmpty(cleanedText))
                {
                    currentNode = GetNextNode(currentNode);
                    continue;
                }

                var fontWeight = 0;
                var stylePattern = @"font-weight:\s*(\d+)";
                var styleMatch = Regex.Match(currentNode.InnerHtml, stylePattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                if (styleMatch.Success)
                {
                    fontWeight = int.Parse(styleMatch.Groups[1].Value);
                }
                var isSubSection = currentNode.InnerHtml.Contains("font-weight:bold")
                                   || currentNode.InnerHtml.Contains("text-decoration:underline")
                                   || fontWeight > 400;
                var dicValue = new StringBuilder();
                if (isSubSection)
                {
                    dicKey = cleanedText;
                }
                else
                {
                    dicValue = new StringBuilder(cleanedText);
                }
                if (sections.TryGetValue(dicKey, out var existingValue))
                {
                    existingValue.Append(cleanedText);
                }
                else
                {
                    sections[dicKey] = dicValue;
                }
            }
           
            currentNode = GetNextNode(currentNode);
        }
        
        return new Dictionary<string, string>(
            sections.ToDictionary(pair => pair.Key, pair => pair.Value.ToString())
        );
    }
    
    private static HtmlNode GetNextNode(HtmlNode currentNode)
    {
        // If there is no next sibling, go up the tree until we find a node with a next sibling
        while (currentNode != null && currentNode.NextSibling == null)
        {
            currentNode = currentNode.ParentNode;
        }

        // Move to the next sibling
        if (currentNode != null)
        {
            currentNode = currentNode.NextSibling;
        }
        
        // Skip div elements with style="page-break-after:always"
        while (currentNode != null &&
               (currentNode.Attributes["style"]?.Value.Contains("page-break-after:always") == true
               || currentNode.Name == "div" && currentNode.InnerText.Contains("Table of Contents")))
        {
            currentNode = currentNode.NextSibling;
        }

        return currentNode;
    }

    private static string[] GetAllHrefs(HtmlNode tableNode)
    {
        // Use XPath to find all 'a' elements
        var linkNodes = tableNode.SelectNodes(".//a[@href]");

        if (linkNodes == null) return new string[0];

        // Extract the 'href' attribute from each 'a' element
        var hrefs = linkNodes
            .Where(node => !node.InnerText.Contains("Item"))
            .Select(node => node.GetAttributeValue("href", string.Empty))
            .Where(href => !string.IsNullOrEmpty(href))
            .Distinct()
            .ToArray();

        return hrefs;
    }
    
    private async Task BuildCompanyEnumAsync(List<CompanyEntity> companyEntities)
    {
        var enumBuilder = new StringBuilder();
        enumBuilder.AppendLine("public enum SecCompanyEnum");
        enumBuilder.AppendLine("{");
        int enumValue = 1;
        foreach (var company in companyEntities)
        {
            // Convert the company name to a valid enum name (remove spaces and special characters)
            var enumName = Regex.Replace(company.Title, "[^a-zA-Z0-9_]", "");

            // Add the enum entry
            enumBuilder.AppendLine($"    [Description(\"{company.Ticker}\")]");
            enumBuilder.AppendLine($"    {enumName} = {enumValue},");
            enumValue++;
        }

        enumBuilder.AppendLine("}");

        // Save the enum 
        await _fileRepository.RemoveAndReplace("DocumentAPI/DTO/SEC/SecCompanyEnum.cs", enumBuilder.ToString());
    }
    
    #endregion
}
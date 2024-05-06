using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using FluentValidation;

namespace DocumentAPI.DTO.SEC;

public class SecDocumentsParserRequest
{
    public SecFormTypeEnum SecDocumentTypeEnum { get; set; }
    public string[] SecDocumentUrls { get; set; }
    public HashSet<string> TargetItemsInDocument { get; set; }
    
    
    public class SecDocumentsParserRequestValidator : AbstractValidator<SecDocumentsParserRequest>
    {
        public SecDocumentsParserRequestValidator()
        {
            RuleFor(x => x.SecDocumentTypeEnum).NotEmpty();
            RuleFor(x => x.SecDocumentUrls).NotEmpty();
        }
    }
}

public class SecDocumentsParserResponse
{
    public string SecDocumentType { get; set; }
    public int RequestedUrls { get; set; }
    public int TotalItems { get; set; }
    public List<SecDocumentData> Sections { get; set; }
    public int CountTotalItems()
    {
        return Sections.Sum(d => d.Items?.Count ?? 0);
    }
    public void CountTotalItemsPerSection()
    {
        if (Sections != null)
        {
            foreach (var section in Sections)
            {
                section.ItemsCnt = section.Items?.Count ?? 0;
            }
        }
    }
}

public class SecDocumentData
{
    public string SecDocumentUrl { get; set; }
    public int ItemsCnt { get; set; }
    [JsonIgnore]
    public HashSet<string> TargetItemsInDocument { get; set; }
    public List<Sec10KIndexDTO> Items { get; set; }
}

public class Sec10KIndexDTO
{
    public string Item{ get; set; }
    public string ItemName { get; set; }
    [JsonIgnore]
    public Sec10KFormSectionEnum? ItemNameEnum { get; set; }
    public string ItemHref { get; set; }
    public Dictionary<string,string> ItemValue { get; set; }
}

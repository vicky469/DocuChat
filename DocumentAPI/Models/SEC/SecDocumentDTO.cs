using System.Text.Json.Serialization;
using FluentValidation;

namespace DocumentAPI.Models.SEC;

public class SecDocumentsParserRequest
{
    public SecFormTypeEnum SecDocumentTypeEnum { get; set; }
    public string[] SecDocumentUrls { get; set; }
    
    
    public class SecDocumentsParserRequestValidator : AbstractValidator<SecDocumentsParserRequest>
    {
        public SecDocumentsParserRequestValidator()
        {
            RuleFor(x => x.SecDocumentTypeEnum).NotEmpty();
            RuleFor(x => x.SecDocumentUrls).NotEmpty();
        }
    }
}

public class SecDocumentData
{
    public string SecDocumentType { get; set; }
    public string SecDocumentUrl { get; set; }
    public List<Sec10KIndexDTO> Items { get; set; }
}
public class Sec10KIndexDTO
{
    public string Item{ get; set; }
    public string ItemName { get; set; }
    [JsonIgnore]
    public Sec10KFormSectionEnum? ItemNameEnum { get; set; }
    public string ItemHref { get; set; }
    public string ItemValue { get; set; }
}

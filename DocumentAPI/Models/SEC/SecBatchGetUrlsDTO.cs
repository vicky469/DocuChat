namespace DocumentAPI.Models.SEC;

public class SecBatchGetUrlsRequest
{
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public SecFormTypeEnum FormTypeEnum { get; set; }
    public SecCompanyEnum CompanyEnum { get; set; }
}
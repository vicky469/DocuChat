namespace DocumentAPI.Models.SEC;

public class SecBatchGetUrlsRequest
{
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public SecFormTypeEnum FormTypeEnum { get; set; }
    public SecCompanyEnum CompanyEnum { get; set; }
}
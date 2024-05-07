namespace DocumentAPI.DTO.SEC;

public class SecBatchGetUrlsRequest
{
    public string StartDate { get; set; }
    public string EndDate { get; set; }
    public SecFormTypeEnum FormTypeEnum { get; set; }
    public List<SecCompanyEnum>? CompanyList { get; set; }
    public int Size { get; set; } = 50;
}


public class SecBatchGetUrlsRequestDTO : SecBatchGetUrlsRequest
{
    public string CIK { get; set; }
}
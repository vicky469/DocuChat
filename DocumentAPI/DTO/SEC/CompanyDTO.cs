namespace DocumentAPI.DTO.SEC;

public class CompanyDTO
{
    public int CIK_Str { get; set; }
    public string CIK_Str_Padded { get; set; }
    public string Ticker { get; set; }
    public string Title { get; set; }
    public SecCompanyEnum CompanyEnum { get; set; }
}

public class Company
{
    public string Name { get; set; }
    public string Ticker { get; set; }
}
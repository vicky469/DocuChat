using Newtonsoft.Json;

namespace DocumentAPI.Infrastructure.Entity;

public class CompanyEntity
{
    [JsonProperty("cik_str")]
    public int CIK_Str { get; set; }
    [JsonProperty("ticker")]
    public string Ticker { get; set; }
    [JsonProperty("title")]
    public string Title { get; set; }
}
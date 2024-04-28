namespace DocumentAPI.Models.SEC;

public class SecSearchResponseDTO
{
    public Filing[] Filings { get; set; }
}
public class Filing
{
    public string LinkToFilingDetails { get; set; }
}

public class SecSearchResponse
{
    public int took { get; set; }
    public bool timed_out { get; set; }
    public _Shards _shards { get; set; }
    public Hits hits { get; set; }
    public string query { get; set; }
}

public class _Shards
{
    public int total { get; set; }
    public int successful { get; set; }
    public int skipped { get; set; }
    public int failed { get; set; }
}

public class Hits
{
    public Total total { get; set; }
    public object max_score { get; set; }
    public List<Hit> hits { get; set; }
    public Aggregations aggregations { get; set; }
}

public class Total
{
    public int value { get; set; }
    public string relation { get; set; }
}

public class Hit
{
    public string _index { get; set; }
    public string _type { get; set; }
    public string _id { get; set; }
    public object _score { get; set; }
    public _Source _source { get; set; }
    public List<long> sort { get; set; }
}

public class _Source
{
    public List<string> ciks { get; set; }
    public string period_ending { get; set; }
    public string root_form { get; set; }
    public List<string> file_num { get; set; }
    public List<string> display_names { get; set; }
    public object xsl { get; set; }
    public string sequence { get; set; }
    public string file_date { get; set; }
    public List<string> biz_states { get; set; }
    public List<string> sics { get; set; }
    public string form { get; set; }
    public string adsh { get; set; }
    public List<string> film_num { get; set; }
    public List<string> biz_locations { get; set; }
    public string file_type { get; set; }
    public string file_description { get; set; }
    public List<string> inc_states { get; set; }
    public List<object> items { get; set; }
}

public class Aggregations
{
    public Entity_Filter entity_filter { get; set; }
    public Sic_Filter sic_filter { get; set; }
    public Biz_States_Filter biz_states_filter { get; set; }
    public Form_Filter form_filter { get; set; }
}

public class Entity_Filter
{
    public int doc_count_error_upper_bound { get; set; }
    public int sum_other_doc_count { get; set; }
    public List<Bucket> buckets { get; set; }
}

public class Bucket
{
    public string key { get; set; }
    public int doc_count { get; set; }
}

public class Sic_Filter
{
    public int doc_count_error_upper_bound { get; set; }
    public int sum_other_doc_count { get; set; }
    public List<Bucket> buckets { get; set; }
}

public class Biz_States_Filter
{
    public int doc_count_error_upper_bound { get; set; }
    public int sum_other_doc_count { get; set; }
    public List<Bucket> buckets { get; set; }
}

public class Form_Filter
{
    public int doc_count_error_upper_bound { get; set; }
    public int sum_other_doc_count { get; set; }
    public List<Bucket> buckets { get; set; }
}
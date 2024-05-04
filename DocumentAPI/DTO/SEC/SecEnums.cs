using System.ComponentModel;

namespace DocumentAPI.DTO.SEC;

public enum SecFormTypeEnum
{
    [Description("10-K")]
    Sec10K = 1,

    [Description("10-Q")]
    Sec10Q = 2,
}

public enum Sec10KFormSectionEnum
{
    [Description("Business")]
    Item1 = 1,

    [Description("Risk Factors")]
    Item1A = 2,

    [Description("Unresolved Staff Comments")]
    Item1B = 3,

    [Description("Cybersecurity")]
    Item1C = 4,

    [Description("Properties")]
    Item2 = 5,

    [Description("Legal Proceedings")]
    Item3 = 6,

    [Description("Mine Safety Disclosures")]
    Item4 = 7,

    [Description("Market for Registrant’s Common Equity, Related Stockholder Matters and Issuer Purchases of Equity Securities")]
    Item5 = 8,

    [Description("Selected Financial Data")]
    Item6 = 9,

    [Description("Management’s Discussion and Analysis of Financial Condition and Results of Operations")]
    Item7 = 10,

    [Description("Quantitative and Qualitative Disclosures about Market Risk")]
    Item7A = 11,

    [Description("Financial Statements and Supplementary Data")]
    Item8 = 12,

    [Description("Changes in and Disagreements with Accountants on Accounting and Financial Disclosure")]
    Item9 = 13,

    [Description("Controls and Procedures")]
    Item9A = 14,

    [Description("Other Information")]
    Item9B = 15,

    [Description("Directors, Executive Officers and Corporate Governance")]
    Item10 = 16,

    [Description("Executive Compensation")]
    Item11 = 17,

    [Description("Security Ownership of Certain Beneficial Owners and Management and Related Stockholder Matters")]
    Item12 = 18,

    [Description("Certain Relationships and Related Transactions, and Director Independence")]
    Item13 = 19,

    [Description("Principal Accountant Fees and Service")]
    Item14 = 20,

    [Description("Exhibits and Financial Statement Schedules")]
    Item15 = 21,
    
    [Description("Summary")]
    Item16 = 22
}



public enum SecCompanyEnum
{
    [Description("Apple Inc.")]
    Apple = 1,

    [Description("Microsoft Corporation")]
    Microsoft = 2,

    [Description("Amazon.com Inc.")]
    Amazon = 3,

    [Description("Alphabet Inc.")]
    Alphabet = 4,

    [Description("Meta Platforms, Inc.")]
    Meta = 5,

    [Description("Tesla Inc.")]
    Tesla = 6,

    [Description("Berkshire Hathaway Inc.")]
    Berkshire = 7,

    [Description("Johnson & Johnson")]
    JohnsonJohnson = 8,

    [Description("JPMorgan Chase & Co.")]
    JPMorgan = 9,

    [Description("Visa Inc.")]
    Visa = 10,

    [Description("Procter & Gamble Co.")]
    ProcterGamble = 11,

    [Description("Mastercard Inc.")]
    Mastercard = 12,

    [Description("UnitedHealth Group Inc.")]
    UnitedHealth = 13,

    [Description("Walmart Inc.")]
    Walmart = 14,

    [Description("Intel Corporation")]
    Intel = 15,

    [Description("Home Depot Inc.")]
    HomeDepot = 16,

    [Description("Verizon Communications Inc.")]
    Verizon = 17,

    [Description("Pfizer Inc.")]
    Pfizer = 18,

    [Description("Coca-Cola Company")]
    CocaCola = 19,

    [Description("AT&T Inc.")]
    ATT = 20,

    [Description("Merck & Co. Inc.")]
    Merck = 21,

    [Description("Netflix Inc.")]
    Netflix = 22,

    [Description("Walt Disney Company")]
    Disney = 23,

    [Description("Cisco Systems Inc.")]
    Cisco = 24,

    [Description("IBM Corporation")]
    IBM = 25,

    [Description("Abbott Laboratories")]
    Abbott = 26,

    [Description("Goldman Sachs Group Inc.")]
    GoldmanSachs = 27,

    [Description("3M Company")]
    MMM = 28,

    [Description("General Electric Company")]
    GE = 29,

    [Description("Boeing Company")]
    Boeing = 30,

    [Description("Caterpillar Inc.")]
    Caterpillar = 31,

    [Description("McDonald's Corporation")]
    McDonalds = 32,

    [Description("Honeywell International Inc.")]
    Honeywell = 33
}
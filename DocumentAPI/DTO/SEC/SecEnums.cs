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





using DominateDocsData.Enums;


public class TitleCompany
{
    public string CompanyName { get; set; } = string.Empty;
    public string OfficerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Property.TitlePolicyTypes PolicyType { get; set; } = Property.TitlePolicyTypes.Single;
    public string SinglePolicyPercent { get; set; } = string.Empty;
}
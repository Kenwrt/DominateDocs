namespace LiquidDocsData.Enums;

public class Trust
{
    public enum Roles
    {
        [System.ComponentModel.Description("Legal")]
        Legal,

        [System.ComponentModel.Description("Advisor")]
        Advisor,

        [System.ComponentModel.Description("Custodian")]
        Custodian,

        [System.ComponentModel.Description("Executor")]
        Executor
    }
}
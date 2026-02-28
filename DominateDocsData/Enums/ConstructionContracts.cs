namespace DominateDocsData.Enums;

public class ConstructionContractors
{
    public enum Roles
    {
        [System.ComponentModel.Description("General Contractor")]
        GeneralContractor,

        [System.ComponentModel.Description("Architect ")]
        Architect,

        [System.ComponentModel.Description("Desinger")]
        Designer,

        [System.ComponentModel.Description("Engineer")]
        Engineer
    }

    public enum HoldbackPaymentOptions
    {
        [System.ComponentModel.Description("Wire Instructions To Be Provided")]
        WireInstructionsToBeProvided,

        [System.ComponentModel.Description("Paid Outside Of Closing")]
        PaidOutsideOfClosing,

        [System.ComponentModel.Description("Deliver to Servicer")]
        DeliverToServicer,

        [System.ComponentModel.Description("Deliver to Construction Fund Control")]
        DeliverToConstructionFundControl,

        [System.ComponentModel.Description("Deliver to Lender")]
        DeliverToLender,

        
        [System.ComponentModel.Description("To Be Net Funded")]
        ToBeNetFunded


    }
        
}
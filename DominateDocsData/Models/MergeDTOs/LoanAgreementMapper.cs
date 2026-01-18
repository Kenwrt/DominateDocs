using DominateDocsData.Models;
using DominateDocsData.Models.MergeDTOs;
using System.Text;

public static class LoanAgreementMapper
{
    public static LoanAgreementDTO ToDto(LoanAgreement loan)
    {
        if (loan == null) return null;

        LoanAgreementDTO loanAgreement = new LoanAgreementDTO
        {
            Id = loan.Id,
            UserId = loan.UserId,
            ReferenceName = loan.ReferenceName,
            LoanNumber = loan.LoanNumber,
            // DocumentSet = loan.DocumentSet,
            PrincipalAmount = loan.PrincipalAmount,
            // LoanType = loan.LoanType,
            //RepaymentSchedule = loan.RepaymentSchedule,
            //RateType = loan.RepaymentType,
            //InterestRate = loan.InterestRate,
            RateIndex = loan.RateIndex,
            IsPrepaymentPenalty = loan.IsPrepaymentPenalty,
            PrepaymentFee = loan.PrepaymentFee,
            //TermInMonths = loan.TermInMonths,
            PrepaymentPremium = loan.PrepaymentPremium,
            ReserveInMonthsToCalculate = loan.ReserveInMonthsToCalculate,
            ReserveSpecificAmount = loan.ReserveSpecificAmount,
            OriginationDate = loan.OriginationDate,
            MaturityDate = loan.MaturityDate,
            IsTaxInsuranceOtherImpounds = loan.IsTaxInsuranceOtherImpounds,
            IsBorrowerResponsibleForServicingFees = loan.IsBorrowerResponsibleForServicingFees,
            ServicingFeeAmount = loan.ServicingFeeAmount,
            IsExitFeeIncluded = loan.IsExitFeeIncluded,
            IsACHDelivery = loan.IsACHDelivery,
            IsRemoveACHDFormFromDocSet = loan.IsRemoveACHDFormFromDocSet,
            ExitFeeAmount = loan.ExitFeeAmount,
            IsConditionalRightToExtend = loan.IsConditionalRightToExtend,
            NumberOfExtensions = loan.NumberOfExtensions,
            NumberOfMonthsForEachExtension = loan.NumberOfMonthsForEachExtension,
            LoanPreparerName = loan.LoanPreparerName,
            LoanPreparerStreetAddress = loan.LoanPreparerStreetAddress,
            LoanPreparerCity = loan.LoanPreparerCity,
            LoanPreparerState = loan.LoanPreparerState,
            LoanPreparerZipCode = loan.LoanPreparerZipCode,
            LoanPreparerCounty = loan.LoanPreparerCounty,
            LoanPreparerEmailAddress = loan.LoanPreparerEmailAddress,
            IsW9TObeIncludedInDocSet = loan.IsW9TObeIncludedInDocSet,
            IsLoanIntendedForSale = loan.IsLoanIntendedForSale,
            LoanSalesInformation = loan.LoanSalesInformation,
            LoanPreparerPhoneNumber = loan.LoanPreparerPhoneNumber,
            LoanPurchaserName = loan.LoanPurchaserName,
            LoanPurchaserStreetAddress = loan.LoanPurchaserStreetAddress,
            LoanPurchaserCity = loan.LoanPurchaserCity,
            LoanPurchaserState = loan.LoanPurchaserState,
            LoanPurchaserZipCode = loan.LoanPurchaserZipCode,
            LoanPurchaserCounty = loan.LoanPurchaserCounty,
            LoanPurchaserEmailAddress = loan.LoanPurchaserEmailAddress,
            LoanPurchaserPhoneNumber = loan.LoanPurchaserPhoneNumber,
            LoanPurchaserAssignees = loan.LoanPurchaserAssignees,
            IsMERSLanuageToBeInserted = loan.IsMERSLanuageToBeInserted,
            IsSignAffidavitAkaRequired = loan.IsSignAffidavitAkaRequired,
            ClosingContactName = loan.ClosingContactName,
            ClosingContactEmail = loan.ClosingContactEmail,
            SignedDate = loan.SignedDate,

            Borrowers = new BorrowerDTO { BorrowerList = loan.Borrowers ?? new() },
            Brokers = new BrokerDTO { BrokerList = loan.Brokers ?? new() },
            Guarantors = new GuarantorDTO { GuarantorList = loan.Guarantors ?? new() },
            Lenders = new LenderDTO { LenderList = loan.Lenders ?? new() },
            Properties = new PropertyRecordDTO { PropertyList = loan.Properties ?? new() },

            PerDiemOption = loan.PerDiemOption,
            FeesToBePaid = loan.FeesToBePaid,
            Status = loan.Status
        };

        StringBuilder sb = new StringBuilder();

        StringBuilder sl = new StringBuilder();

        // build loanDescriptors for Borrowers
        foreach (var borrower in loanAgreement.Borrowers.BorrowerList)
        {
            if (sb.Length > 0) sb.Append(", ");
            if (sl.Length > 0) sl.Append("\n\n");

            if (borrower.EntityType == DominateDocsData.Enums.Entity.Types.Individual)
            {
                sb.Append($"{borrower.EntityName} a {borrower.EntityType.ToString()}");
            }
            else
            {
                sb.Append($"{borrower.EntityName} a {borrower.EntityStructure.ToString()}");
            }

            if (borrower.EntityStructure != DominateDocsData.Enums.Entity.Structures.Trust)
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nName: {borrower.ContactName}");
                sl.Append($"\nTitle: {borrower.ContactsRole.ToString()}");
                sl.Append($"\nDate: __________________________");
            }
            else
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nTrustee: {borrower.ContactName}");
                sl.Append($"\nDate: __________________________");
            }
        }

        loanAgreement.Borrowers.EntityDescriptors = sb.ToString();

        // build Signature Lines for Borrowers
        loanAgreement.Borrowers.EntitySignatureLines = sl.ToString();

        // build loanDescriptors for Lenders
        foreach (var lender in loanAgreement.Lenders.LenderList)
        {
            if (sb.Length > 0) sb.Append(", ");
            if (sl.Length > 0) sl.Append("\n\n");

            if (lender.EntityType == DominateDocsData.Enums.Entity.Types.Individual)
            {
                sb.Append($"{lender.EntityName} a {lender.EntityType.ToString()}");
            }
            else
            {
                sb.Append($"{lender.EntityName} a {lender.EntityStructure.ToString()}");
            }

            if (lender.EntityStructure != DominateDocsData.Enums.Entity.Structures.Trust)
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nName: {lender.ContactName}");
                sl.Append($"\nTitle: {lender.ContactsRole.ToString()}");
                sl.Append($"\nDate: __________________________");
            }
            else
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nTrustee: {lender.ContactName}");
                sl.Append($"\nDate: __________________________");
            }
        }

        loanAgreement.Lenders.EntityDescriptors = sb.ToString();

        // build Signature Lines for Lenders
        loanAgreement.Lenders.EntitySignatureLines = sl.ToString();

        // build loanDescriptors for Guarantors
        foreach (var guarantor in loanAgreement.Guarantors.GuarantorList)
        {
            if (sb.Length > 0) sb.Append(", ");
            if (sl.Length > 0) sl.Append("\n\n");

            if (guarantor.EntityType == DominateDocsData.Enums.Entity.Types.Individual)
            {
                sb.Append($"{guarantor.EntityName} a {guarantor.EntityType.ToString()}");
            }
            else
            {
                sb.Append($"{guarantor.EntityName} a {guarantor.EntityStructure.ToString()}");
            }

            if (guarantor.EntityStructure != DominateDocsData.Enums.Entity.Structures.Trust)
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nName: {guarantor.ContactName}");
                sl.Append($"\nTitle: {guarantor.ContactsRole.ToString()}");
                sl.Append($"\nDate: __________________________");
            }
            else
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nTrustee: {guarantor.ContactName}");
                sl.Append($"\nDate: __________________________");
            }
        }

        loanAgreement.Guarantors.EntityDescriptors = sb.ToString();

        // build Signature Lines for Guarantors
        loanAgreement.Guarantors.EntitySignatureLines = sl.ToString();

        // build loanDescriptors for Brokers
        foreach (var broker in loanAgreement.Brokers.BrokerList)
        {
            if (sb.Length > 0) sb.Append(", ");
            if (sl.Length > 0) sl.Append("\n\n");

            if (broker.EntityType == DominateDocsData.Enums.Entity.Types.Individual)
            {
                sb.Append($"{broker.EntityName} a {broker.EntityType.ToString()}");
            }
            else
            {
                sb.Append($"{broker.EntityName} a {broker.EntityStructure.ToString()}");
            }

            if (broker.EntityStructure != DominateDocsData.Enums.Entity.Structures.Trust)
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nName: {broker.ContactName}");
                sl.Append($"\nTitle: {broker.ContactsRole.ToString()}");
                sl.Append($"\nDate: __________________________");
            }
            else
            {
                sl.Append(sb.ToString());
                sl.Append($"\nBy: ____________________________");
                sl.Append($"\nTrustee: {broker.ContactName}");
                sl.Append($"\nDate: __________________________");
            }
        }

        loanAgreement.Brokers.EntityDescriptors = sb.ToString();

        // build Signature Lines for Brokers
        loanAgreement.Brokers.EntitySignatureLines = sl.ToString();

        return loanAgreement;
    }
}
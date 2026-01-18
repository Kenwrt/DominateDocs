using DocumentManager.Helpers;
using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class LoanAgreementMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(LoanAgreement x)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Loan.LoanNumber}", x.LoanNumber.ToString()),

                new KeyValuePair<string, string>("{Loan.PrincipleAmount}", x.PrincipalAmount.ToString()),
                new KeyValuePair<string, string>("{Loan.OriginationDate}", x.OriginationDate.ToString() ),
                 new KeyValuePair<string, string>("{Loan.MaturityDate}", x.MaturityDate.ToString() ),
                new KeyValuePair<string, string>("{Loan.AmountSpelledOut}", ToWords.DollarsToWords(x.PrincipalAmount) ),
                //new KeyValuePair<string, string>("{Loan.InterestRate}", x.InterestRate.ToString()),
               // new KeyValuePair<string, string>("{Loan.InterestRateSpelledOut}", ToWords.InterestRateToWords(x.InterestRate)),
                new KeyValuePair<string, string>("{Loan.IsTaxInsuranceOtherImpounds}", x.IsTaxInsuranceOtherImpounds.ToString()),

                new KeyValuePair<string, string>("{Loan.IsBorrowerResponsibleForServicingFees}", x.IsBorrowerResponsibleForServicingFees.ToString()),
                new KeyValuePair<string, string>("{Loan.ServicingFeeAmount}", x.ServicingFeeAmount.ToString()),

                new KeyValuePair<string, string>("{Loan.IsExitFeeIncluded}", x.IsExitFeeIncluded.ToString()),
                new KeyValuePair<string, string>("{Loan.IsACHDelivery}", x.IsACHDelivery.ToString()),
                new KeyValuePair<string, string>("{Loan.IsRemoveACHDFormFromDocSet}", x.IsRemoveACHDFormFromDocSet.ToString()),
                new KeyValuePair<string, string>("{Loan.ExitFeeAmount}", x.ExitFeeAmount.ToString()),

                new KeyValuePair<string, string>("{Loan.IsConditionalRightToExtend}", x.IsConditionalRightToExtend.ToString()),
                new KeyValuePair<string, string>("{Loan.NumberOfExtensions}", x.NumberOfExtensions.ToString()),
                new KeyValuePair<string, string>("{Loan.NumberOfMonthsForEachExtension}", x.NumberOfMonthsForEachExtension.ToString()),

                new KeyValuePair<string, string>("{Loan.PreparerName}", x.LoanPreparerName.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerStreetAddress}", x.LoanPreparerStreetAddress.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerCity}", x.LoanPreparerCity.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerState}", x.LoanPreparerState.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerZipCode}", x.LoanPreparerZipCode.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerCounty}", x.LoanPreparerCounty.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerEmailAddress}", x.LoanPreparerEmailAddress.ToString()),

                new KeyValuePair<string, string>("{Loan.IsW9TObeIncludedInDocSet}", x.IsW9TObeIncludedInDocSet.ToString()),
                new KeyValuePair<string, string>("{Loan.IsLoanIntendedForSale}", x.IsLoanIntendedForSale.ToString()),

                new KeyValuePair<string, string>("{Loan.SalesInformation}", x.LoanSalesInformation.ToString()),
                new KeyValuePair<string, string>("{Loan.PreparerPhoneNumber}", x.LoanPreparerPhoneNumber.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserName}", x.LoanPurchaserName.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserStreetAddress}", x.LoanPurchaserStreetAddress.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserCity}", x.LoanPurchaserCity.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserState}", x.LoanPurchaserState.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserZipCode}", x.LoanPurchaserZipCode.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserCounty}", x.LoanPurchaserCounty.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserEmailAddress}", x.LoanPurchaserEmailAddress.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserPhoneNumber}", x.LoanPurchaserPhoneNumber.ToString()),
                new KeyValuePair<string, string>("{Loan.PurchaserAssignees}", x.LoanPurchaserAssignees.ToString()),
                new KeyValuePair<string, string>("{Loan.IsMERSLanuageToBeInserted}", x.IsMERSLanuageToBeInserted.ToString()),
                new KeyValuePair<string, string>("{Loan.IsSignAffidavitAkaRequired}", x.IsSignAffidavitAkaRequired.ToString()),

                new KeyValuePair<string, string>("{Loan.ClosingContactName}", x.ClosingContactName.ToString()),
                new KeyValuePair<string, string>("{Loan.ClosingContactEmail}", x.ClosingContactEmail.ToString()),

                new KeyValuePair<string, string>("{Loan.SignedDate}", x.SignedDate.ToString()),
                new KeyValuePair<string, string>("{Loan.PerDiemOption}", x.PerDiemOption.ToString()),
               // new KeyValuePair<string, string>("{Loan.RateType}", x.RepaymentType.ToString()),
                new KeyValuePair<string, string>("{Loan.RateIndex}", x.RateIndex.ToString()),

              //  new KeyValuePair<string, string>("{Loan.PaymentType}", x.PaymentType.ToString()),
                new KeyValuePair<string, string>("{Loan.ReserveType}", x.ReserveType.ToString()),
                new KeyValuePair<string, string>("{Loan.FeesPaidOption}", x.FeesPaidToOption.ToString()),
                new KeyValuePair<string, string>("{Loan.ExtenstionFeeType}", x.ExtenstionFeeType.ToString()),
              //  new KeyValuePair<string, string>("{Loan.RepaymentSchedule}", x.RepaymentSchedule.ToString()),
                new KeyValuePair<string, string>("{Loan.IsPrepaymentPenality}", x.IsPrepaymentPenalty.ToString()),

                new KeyValuePair<string, string>("{Loan.PrepaymentFee}", x.PrepaymentFee.ToString()),
              //  new KeyValuePair<string, string>("{Loan.TermsInMonths}", x.TermInMonths.ToString()),
                new KeyValuePair<string, string>("{Loan.PrepaymentPremium}", x.PrepaymentPremium.ToString()),
                new KeyValuePair<string, string>("{Loan.ReserveInMonthsToCalculate}", x.ReserveInMonthsToCalculate.ToString()),
                new KeyValuePair<string, string>("{Loan.ReserveSpecificAmount}", x.ReserveSpecificAmount.ToString()),
                //new KeyValuePair<string, string>("{Loan.Type}", x.LoanType.ToString()),
                new KeyValuePair<string, string>("{Loan.FeeToBePaidList}", ""),
                //new KeyValuePair<string, string>("{Loan.Lenders}", x.LendersFormatted),
                //new KeyValuePair<string, string>("{Loan.Borrowers}", x.BorrowersFormatted),
                //new KeyValuePair<string, string>("{Loan.Brokers}", x.BrokersFormatted),
                //new KeyValuePair<string, string>("{Loan.Guarantors}", x.GuarantorsFormatted),
                //new KeyValuePair<string, string>("{Loan.Properties}",x.PropertiesFormatted),
                //new KeyValuePair<string, string>("{Loan.Signatures}", x.SignatureLinesFormatted),
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}
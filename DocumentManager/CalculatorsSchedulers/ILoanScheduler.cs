using DominateDocsData.Enums;
using DominateDocsData.Models;

namespace DocumentManager.CalculatorsSchedulers;

public interface ILoanScheduler
{
    PaymentSchedule GenerateFixed(decimal principal, decimal annualRatePercent, decimal downPaymentPercent, DateTime startDate, DateTime endDate, Payment.AmortizationTypes amortizationType, int? amortizationTermMonths = null);

    PaymentSchedule GenerateVariable(decimal principal, decimal downPaymentPercent, DateTime startDate, DateTime endDate, Payment.AmortizationTypes amortizationType, List<RateChange> rateSchedule, int? amortizationTermMonths = null);
}
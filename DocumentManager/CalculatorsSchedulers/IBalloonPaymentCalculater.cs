using DominateDocsData.Models;

namespace DocumentManager.CalculatorsSchedulers;

public interface IBalloonPaymentCalculater
{
    BalloonPayments Generate(decimal principal, decimal annualRatePercent, int amortizationTermMonths, int balloonTermMonths, DateOnly firstPaymentDate, int paymentsPerYear = 12);
}
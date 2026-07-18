namespace SmartCar.Domain.Policies;

public sealed record SettlementCalculation(
    decimal GrossRental,
    decimal PlatformFee,
    decimal PaymentGatewayFee,
    decimal RefundAmount,
    decimal CompensationAmount,
    decimal OwnerPayout);

public static class SettlementCalculationPolicy
{
    public static SettlementCalculation Calculate(
        decimal grossRental,
        decimal platformFee,
        decimal paymentGatewayFee,
        decimal refundAmount,
        decimal compensationAmount)
    {
        if (grossRental < 0 || platformFee < 0 || paymentGatewayFee < 0 || refundAmount < 0 || compensationAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossRental), "Các thành phần đối soát không được âm.");

        var deductions = platformFee + paymentGatewayFee + refundAmount + compensationAmount;
        return new SettlementCalculation(
            grossRental,
            platformFee,
            paymentGatewayFee,
            refundAmount,
            compensationAmount,
            Math.Max(0m, grossRental - deductions));
    }
}

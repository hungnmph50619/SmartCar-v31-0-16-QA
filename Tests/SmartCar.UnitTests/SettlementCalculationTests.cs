using SmartCar.Domain.Policies;

namespace SmartCar.UnitTests;

public class SettlementCalculationTests
{
    [Fact]
    public void CalculatesOwnerPayout_FromServerSideComponents()
    {
        var result = SettlementCalculationPolicy.Calculate(2_200_000m, 300_000m, 20_000m, 100_000m, 50_000m);
        Assert.Equal(1_730_000m, result.OwnerPayout);
    }

    [Fact]
    public void DeductionsAboveGross_CannotCreateNegativePayout()
    {
        var result = SettlementCalculationPolicy.Calculate(1_000_000m, 300_000m, 100_000m, 900_000m, 100_000m);
        Assert.Equal(0m, result.OwnerPayout);
    }

    [Fact]
    public void NegativeClientLikeValue_IsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SettlementCalculationPolicy.Calculate(1_000_000m, 100_000m, -1m, 0m, 0m));
    }
}

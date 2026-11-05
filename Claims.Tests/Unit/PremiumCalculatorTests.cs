using Claims.Domain;
using Claims.Services;
using Xunit;

namespace Claims.Tests.Unit;

public class PremiumCalculatorTests
{
    private static readonly DateTime Start = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly PremiumCalculator _calculator = new();

    [Theory]
    [InlineData(CoverType.Yacht, 1375)]
    [InlineData(CoverType.PassengerShip, 1500)]
    [InlineData(CoverType.Tanker, 1875)]
    [InlineData(CoverType.ContainerShip, 1625)]
    [InlineData(CoverType.BulkCarrier, 1625)]
    public void One_day_costs_the_day_rate_of_the_type(CoverType type, double expected)
    {
        Assert.Equal((decimal)expected, _calculator.Compute(Start, Start.AddDays(1), type));
    }

    // Each row sits on a boundary between pricing bands: last day of a band, first day of the next.
    [Theory]
    [InlineData(CoverType.Yacht, 30, 41_250)]        // 30 x 1375
    [InlineData(CoverType.Yacht, 31, 42_556.25)]     // + 1375 x 0.95
    [InlineData(CoverType.Yacht, 180, 237_187.5)]    // 41250 + 150 x 1306.25
    [InlineData(CoverType.Yacht, 181, 238_452.5)]    // + 1375 x 0.92
    [InlineData(CoverType.Yacht, 365, 471_212.5)]    // 237187.5 + 185 x 1265
    [InlineData(CoverType.Tanker, 30, 56_250)]       // 30 x 1875
    [InlineData(CoverType.Tanker, 31, 58_087.5)]     // + 1875 x 0.98
    [InlineData(CoverType.Tanker, 180, 331_875)]     // 56250 + 150 x 1837.5
    [InlineData(CoverType.Tanker, 181, 333_693.75)]  // + 1875 x 0.97
    [InlineData(CoverType.Tanker, 365, 668_343.75)]  // 331875 + 185 x 1818.75
    public void Later_days_are_discounted_progressively(CoverType type, int days, double expected)
    {
        Assert.Equal((decimal)expected, _calculator.Compute(Start, Start.AddDays(days), type));
    }

    [Fact]
    public void A_started_day_is_charged_in_full()
    {
        var premium = _calculator.Compute(Start, Start.AddHours(36), CoverType.Yacht);

        Assert.Equal(2 * 1375m, premium);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void An_empty_or_negative_period_costs_nothing(int days)
    {
        Assert.Equal(0m, _calculator.Compute(Start, Start.AddDays(days), CoverType.Tanker));
    }
}

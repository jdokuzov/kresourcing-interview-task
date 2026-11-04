using Claims.Domain;

namespace Claims.Services;

/// <summary>
/// Premium = day rate x days, where the day rate depends on the vessel type and
/// later days of the period are discounted progressively.
/// </summary>
public sealed class PremiumCalculator : IPremiumCalculator
{
    private const decimal BaseDayRate = 1250m;
    private const int FullPriceDays = 30;
    private const int FirstDiscountDays = 150;

    public decimal Compute(DateTime startDate, DateTime endDate, CoverType coverType)
    {
        var days = Math.Max(0, (int)Math.Ceiling((endDate - startDate).TotalDays));

        var fullPriceDays = Math.Min(days, FullPriceDays);
        var firstDiscountDays = Math.Min(days - fullPriceDays, FirstDiscountDays);
        var secondDiscountDays = days - fullPriceDays - firstDiscountDays;

        var (firstDiscount, secondDiscount) = Discounts(coverType);
        var dayRate = BaseDayRate * RateMultiplier(coverType);

        return dayRate * fullPriceDays
            + dayRate * (1 - firstDiscount) * firstDiscountDays
            + dayRate * (1 - secondDiscount) * secondDiscountDays;
    }

    private static decimal RateMultiplier(CoverType coverType) => coverType switch
    {
        CoverType.Yacht => 1.1m,
        CoverType.PassengerShip => 1.2m,
        CoverType.Tanker => 1.5m,
        _ => 1.3m
    };

    /// <summary>The second discount is on top of the first: 5% + 3% for yachts, 2% + 1% for other types.</summary>
    private static (decimal First, decimal Second) Discounts(CoverType coverType) =>
        coverType == CoverType.Yacht ? (0.05m, 0.08m) : (0.02m, 0.03m);
}

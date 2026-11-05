using Claims.Domain;

namespace Claims.Services;

/// <summary>Computes the premium of a cover.</summary>
public interface IPremiumCalculator
{
    /// <returns>The premium for the period. Every started day is charged in full; an empty or negative period costs nothing.</returns>
    decimal Compute(DateTime startDate, DateTime endDate, CoverType coverType);
}

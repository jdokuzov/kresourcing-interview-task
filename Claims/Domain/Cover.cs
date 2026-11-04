namespace Claims.Domain;

/// <summary>An insurance policy for a single vessel over a limited period.</summary>
public class Cover
{
    public required string Id { get; set; }

    /// <summary>First day of the insurance period (UTC).</summary>
    public DateTime StartDate { get; set; }

    /// <summary>End of the insurance period (UTC).</summary>
    public DateTime EndDate { get; set; }

    public CoverType Type { get; set; }

    public decimal Premium { get; set; }
}

public enum CoverType
{
    Yacht = 0,
    PassengerShip = 1,
    ContainerShip = 2,
    BulkCarrier = 3,
    Tanker = 4
}

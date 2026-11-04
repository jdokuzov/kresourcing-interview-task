namespace Claims.Domain;

/// <summary>A damage claim filed against a <see cref="Cover"/>.</summary>
public class Claim
{
    public required string Id { get; set; }

    public required string CoverId { get; set; }

    /// <summary>Date the claim was created (UTC). Must fall within the period of the related cover.</summary>
    public DateTime Created { get; set; }

    public required string Name { get; set; }

    public ClaimType Type { get; set; }

    public decimal DamageCost { get; set; }
}

public enum ClaimType
{
    Collision = 0,
    Grounding = 1,
    BadWeather = 2,
    Fire = 3
}

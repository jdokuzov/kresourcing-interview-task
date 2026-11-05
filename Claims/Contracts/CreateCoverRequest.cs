using System.ComponentModel.DataAnnotations;
using Claims.Domain;

namespace Claims.Contracts;

/// <summary>Payload for creating a cover. The id and premium are determined by the server.</summary>
public sealed record CreateCoverRequest
{
    public required DateTime StartDate { get; init; }

    public required DateTime EndDate { get; init; }

    [EnumDataType(typeof(CoverType))]
    public required CoverType Type { get; init; }
}

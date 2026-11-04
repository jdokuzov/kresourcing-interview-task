using Microsoft.EntityFrameworkCore;

namespace Claims.Auditing;

[Index(nameof(MessageId), IsUnique = true)]
public class ClaimAudit
{
    public int Id { get; set; }

    /// <summary>The <see cref="AuditEvent.Id"/> this record came from. Null on records older than the outbox.</summary>
    public string? MessageId { get; set; }

    public required string ClaimId { get; set; }

    public DateTime Created { get; set; }

    public required string HttpRequestType { get; set; }
}

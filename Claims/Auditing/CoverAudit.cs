using Microsoft.EntityFrameworkCore;

namespace Claims.Auditing;

[Index(nameof(MessageId), IsUnique = true)]
public class CoverAudit
{
    public int Id { get; set; }

    /// <summary>The <see cref="AuditEvent.Id"/> this record came from. Null on records older than the outbox.</summary>
    public string? MessageId { get; set; }

    public required string CoverId { get; set; }

    public DateTime Created { get; set; }

    public required string HttpRequestType { get; set; }
}

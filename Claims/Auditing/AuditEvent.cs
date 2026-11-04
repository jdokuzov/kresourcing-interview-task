namespace Claims.Auditing;

public enum AuditedEntity
{
    Claim = 0,
    Cover = 1
}

/// <summary>
/// A change to a claim or cover on its way to the audit database. It is stored in the outbox
/// and then sent as the Service Bus message. <see cref="Id"/> identifies it along the whole way,
/// which is what makes delivering it twice harmless.
/// </summary>
public class AuditEvent
{
    public required string Id { get; set; }

    public AuditedEntity EntityType { get; set; }

    public required string EntityId { get; set; }

    public required string HttpRequestType { get; set; }

    public DateTime Created { get; set; }
}

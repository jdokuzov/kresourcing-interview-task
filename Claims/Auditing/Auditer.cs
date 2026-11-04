using Claims.Persistence;

namespace Claims.Auditing;

/// <summary>Adds audit events to the outbox, from where <see cref="OutboxRelay"/> publishes them.</summary>
public sealed class Auditer(IRepository<AuditEvent> outbox, TimeProvider timeProvider) : IAuditer
{
    public void AuditClaim(string claimId, string httpRequestType) =>
        Add(AuditedEntity.Claim, claimId, httpRequestType);

    public void AuditCover(string coverId, string httpRequestType) =>
        Add(AuditedEntity.Cover, coverId, httpRequestType);

    private void Add(AuditedEntity entityType, string entityId, string httpRequestType) =>
        outbox.Add(new AuditEvent
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = entityType,
            EntityId = entityId,
            HttpRequestType = httpRequestType,
            Created = timeProvider.GetUtcNow().UtcDateTime
        });
}

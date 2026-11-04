using Microsoft.EntityFrameworkCore;

namespace Claims.Auditing;

/// <summary>
/// Writes an audit event to the audit database, once. Service Bus delivers at least once, so an
/// event that is already there is ignored. If two deliveries race, the unique index rejects one;
/// its message is then redelivered and ignored here.
/// </summary>
public sealed class AuditWriter(AuditContext context)
{
    public async Task WriteAsync(AuditEvent auditEvent, CancellationToken cancellationToken)
    {
        if (auditEvent.EntityType == AuditedEntity.Claim)
        {
            if (await context.ClaimAudits.AnyAsync(audit => audit.MessageId == auditEvent.Id, cancellationToken))
            {
                return;
            }

            context.Add(new ClaimAudit
            {
                MessageId = auditEvent.Id,
                ClaimId = auditEvent.EntityId,
                HttpRequestType = auditEvent.HttpRequestType,
                Created = auditEvent.Created
            });
        }
        else
        {
            if (await context.CoverAudits.AnyAsync(audit => audit.MessageId == auditEvent.Id, cancellationToken))
            {
                return;
            }

            context.Add(new CoverAudit
            {
                MessageId = auditEvent.Id,
                CoverId = auditEvent.EntityId,
                HttpRequestType = auditEvent.HttpRequestType,
                Created = auditEvent.Created
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}

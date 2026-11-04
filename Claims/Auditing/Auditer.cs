using System.Threading.Channels;

namespace Claims.Auditing;

/// <summary>
/// Puts audit records on an in-memory queue, so requests do not wait for the audit database.
/// <see cref="AuditWorker"/> does the actual writing.
/// </summary>
public sealed class Auditer(Channel<IAuditRecord> queue, TimeProvider timeProvider) : IAuditer
{
    public ValueTask AuditClaimAsync(string claimId, string httpRequestType) =>
        queue.Writer.WriteAsync(new ClaimAudit
        {
            ClaimId = claimId,
            HttpRequestType = httpRequestType,
            Created = timeProvider.GetUtcNow().UtcDateTime
        });

    public ValueTask AuditCoverAsync(string coverId, string httpRequestType) =>
        queue.Writer.WriteAsync(new CoverAudit
        {
            CoverId = coverId,
            HttpRequestType = httpRequestType,
            Created = timeProvider.GetUtcNow().UtcDateTime
        });
}

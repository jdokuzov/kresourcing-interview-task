namespace Claims.Auditing;

/// <summary>Writes audit records to the audit database as part of the request.</summary>
public sealed class Auditer(AuditContext auditContext) : IAuditer
{
    public async ValueTask AuditClaimAsync(string claimId, string httpRequestType)
    {
        auditContext.Add(new ClaimAudit
        {
            ClaimId = claimId,
            HttpRequestType = httpRequestType,
            Created = DateTime.UtcNow
        });
        await auditContext.SaveChangesAsync();
    }

    public async ValueTask AuditCoverAsync(string coverId, string httpRequestType)
    {
        auditContext.Add(new CoverAudit
        {
            CoverId = coverId,
            HttpRequestType = httpRequestType,
            Created = DateTime.UtcNow
        });
        await auditContext.SaveChangesAsync();
    }
}

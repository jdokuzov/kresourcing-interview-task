namespace Claims.Auditing;

/// <summary>
/// Records which requests changed claims and covers. Nothing is written until
/// <see cref="Persistence.IUnitOfWork.SaveChangesAsync"/>, so the audit record is saved
/// together with the change it describes, or not at all.
/// </summary>
public interface IAuditer
{
    void AuditClaim(string claimId, string httpRequestType);

    void AuditCover(string coverId, string httpRequestType);
}

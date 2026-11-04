namespace Claims.Auditing;

/// <summary>Records which requests changed claims and covers.</summary>
public interface IAuditer
{
    /// <summary>Records a change to a claim.</summary>
    ValueTask AuditClaimAsync(string claimId, string httpRequestType);

    /// <summary>Records a change to a cover.</summary>
    ValueTask AuditCoverAsync(string coverId, string httpRequestType);
}

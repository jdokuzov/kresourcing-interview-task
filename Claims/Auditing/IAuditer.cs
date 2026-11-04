namespace Claims.Auditing;

/// <summary>Records which requests changed claims and covers.</summary>
public interface IAuditer
{
    /// <summary>Records a change to a claim. Completes before the record is persisted.</summary>
    ValueTask AuditClaimAsync(string claimId, string httpRequestType);

    /// <summary>Records a change to a cover. Completes before the record is persisted.</summary>
    ValueTask AuditCoverAsync(string coverId, string httpRequestType);
}

/// <summary>Marks an audit entity that can travel through the audit queue.</summary>
public interface IAuditRecord;

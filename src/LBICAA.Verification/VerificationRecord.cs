
namespace LBICAA.Domain.Verification;

public enum VerificationMethod
{
    ManualReview,
    AutomatedCheck,
    CrossSourceComparison,
    LegalReview,
    DataValidation
}

public enum VerificationOutcome
{
    Confirmed,
    PartiallyConfirmed,
    Inconclusive,
    Disputed,
    Rejected
}

public sealed record VerificationRecord
{
    public Guid Id { get; }
    public Guid ClaimId { get; }

    public VerificationMethod Method { get; }
    public VerificationOutcome Outcome { get; }

    // Examples: "system:LBICAA.Crawler"
    // or "reviewer:registered-reviewer-id"
    public string VerifierReference { get; }

    public string? Notes { get; }
    public DateTimeOffset VerifiedAt { get; }

    public VerificationRecord(
        Guid id,
        Guid claimId,
        VerificationMethod method,
        VerificationOutcome outcome,
        string verifierReference,
        DateTimeOffset verifiedAt,
        string? notes = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Verification ID is required.");

        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim ID is required.");

        if (string.IsNullOrWhiteSpace(verifierReference))
            throw new ArgumentException("Verifier reference is required.");

        Id = id;
        ClaimId = claimId;
        Method = method;
        Outcome = outcome;
        VerifierReference = verifierReference;
        VerifiedAt = verifiedAt;
        Notes = notes;
    }
}

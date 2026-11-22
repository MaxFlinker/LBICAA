using LBICAA.Domain.Verification;

namespace LBICAA.Application.Verification.RecordVerification;

public sealed record RecordVerificationRequest(
    Guid ClaimId,
    VerificationMethod Method,
    VerificationOutcome Outcome,
    string VerifierReference,
    string? Notes = null);

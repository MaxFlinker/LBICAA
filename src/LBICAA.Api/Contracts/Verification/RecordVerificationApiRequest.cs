using LBICAA.Domain.Verification;

namespace LBICAA.Api.Contracts.Verification;

public sealed record RecordVerificationApiRequest(
    VerificationMethod Method,
    VerificationOutcome Outcome,
    string VerifierReference,
    string? Notes = null);

using LBICAA.Domain.Claims;

namespace LBICAA.Application.Claims.CreateClaim;

public sealed record CreateClaimRequest(
    string SubjectId,
    string Predicate,
    ClaimValue Object,
    ClaimNature Nature,
    ConfidenceLevel Confidence,
    TemporalValidity Validity);

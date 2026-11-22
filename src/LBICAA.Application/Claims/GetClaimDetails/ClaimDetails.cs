using LBICAA.Domain.Claims;
using EvidenceEntity = LBICAA.Domain.Evidence.Evidence;
using LBICAA.Domain.Verification;

namespace LBICAA.Application.Claims.GetClaimDetails;

public sealed record ClaimDetails(
    Claim Claim,
    IReadOnlyList<EvidenceEntity> Evidence,
    IReadOnlyList<VerificationRecord> VerificationHistory);

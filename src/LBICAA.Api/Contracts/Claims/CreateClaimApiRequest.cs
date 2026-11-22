using LBICAA.Domain.Claims;

namespace LBICAA.Api.Contracts.Claims;

public sealed record CreateClaimApiRequest(
    string SubjectId,
    string Predicate,
    ClaimValueApiRequest Object,
    ClaimNature Nature,
    ConfidenceLevel Confidence,
    TemporalValidityApiRequest Validity);

public sealed record ClaimValueApiRequest(
    ClaimValueKind Kind,
    string Value);

public sealed record TemporalValidityApiRequest(
    DateOnly? ValidFrom,
    DateOnly? ValidUntil);

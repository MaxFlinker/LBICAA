namespace LBICAA.Api.Contracts.Claims;

public sealed record ClaimDetailsApiResponse(
    Guid Id,
    string SubjectId,
    string Predicate,
    ClaimValueApiResponse Object,
    string Nature,
    string Status,
    string Confidence,
    TemporalValidityApiResponse Validity,
    DateTimeOffset RecordedAt,
    IReadOnlyList<EvidenceApiResponse> Evidence,
    IReadOnlyList<VerificationApiResponse> VerificationHistory);

public sealed record ClaimValueApiResponse(
    string Kind,
    string Value);

public sealed record TemporalValidityApiResponse(
    DateOnly? ValidFrom,
    DateOnly? ValidUntil);

public sealed record EvidenceApiResponse(
    Guid Id,
    Guid SourceId,
    string Locator,
    string? Excerpt,
    string Assessment,
    DateTimeOffset RecordedAt);

public sealed record VerificationApiResponse(
    Guid Id,
    string Method,
    string Outcome,
    string VerifierReference,
    string? Notes,
    DateTimeOffset VerifiedAt);

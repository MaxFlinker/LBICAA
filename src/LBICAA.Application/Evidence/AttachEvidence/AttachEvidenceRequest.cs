using LBICAA.Domain.Evidence;

namespace LBICAA.Application.Evidence.AttachEvidence;

public sealed record AttachEvidenceRequest(
    Guid ClaimId,
    Guid SourceId,
    string Locator,
    EvidenceAssessment Assessment,
    string? Excerpt = null);

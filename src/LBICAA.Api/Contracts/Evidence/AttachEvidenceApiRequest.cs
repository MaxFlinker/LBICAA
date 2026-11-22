using LBICAA.Domain.Evidence;

namespace LBICAA.Api.Contracts.Evidence;

public sealed record AttachEvidenceApiRequest(
    Guid SourceId,
    string Locator,
    EvidenceAssessment Assessment,
    string? Excerpt = null);

namespace LBICAA.Domain.Evidence;

public enum EvidenceAssessment
{
    Supports,
    Contradicts,
    Contextual
}

public sealed record Evidence
{
    public Guid Id { get; }
    public Guid ClaimId { get; }
    public Guid SourceId { get; }

    // Exact location within the source:
    // page number, paragraph, section, dataset row, etc.
    public string Locator { get; }

    // Optional short excerpt. The locator and source remain
    // authoritative; excerpts should respect source licences.
    public string? Excerpt { get; }

    public EvidenceAssessment Assessment { get; }

    public DateTimeOffset RecordedAt { get; }

    public Evidence(
        Guid id,
        Guid claimId,
        Guid sourceId,
        string locator,
        EvidenceAssessment assessment,
        DateTimeOffset recordedAt,
        string? excerpt = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Evidence ID is required.");

        if (claimId == Guid.Empty)
            throw new ArgumentException("Claim ID is required.");

        if (sourceId == Guid.Empty)
            throw new ArgumentException("Source ID is required.");

        if (string.IsNullOrWhiteSpace(locator))
            throw new ArgumentException("Evidence locator is required.");

        Id = id;
        ClaimId = claimId;
        SourceId = sourceId;
        Locator = locator;
        Excerpt = excerpt;
        Assessment = assessment;
        RecordedAt = recordedAt;
    }
}

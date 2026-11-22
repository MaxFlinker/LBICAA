using LBICAA.Application.Abstractions.Persistence;
using EvidenceEntity = LBICAA.Domain.Evidence.Evidence;
using LBICAA.Domain.Evidence;

namespace LBICAA.Application.Evidence.AttachEvidence;

public sealed class AttachEvidenceHandler(
    IClaimRepository claimRepository,
    ISourceRepository sourceRepository,
    IEvidenceRepository evidenceRepository)
{
    public async Task<EvidenceEntity> HandleAsync(
        AttachEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claim = await claimRepository.GetByIdAsync(
            request.ClaimId,
            cancellationToken);

        if (claim is null)
        {
            throw new KeyNotFoundException(
                $"Claim '{request.ClaimId}' was not found.");
        }

        var source = await sourceRepository.GetByIdAsync(
            request.SourceId,
            cancellationToken);

        if (source is null)
        {
            throw new KeyNotFoundException(
                $"Source '{request.SourceId}' was not found.");
        }

        var evidence = new EvidenceEntity(
            id: Guid.NewGuid(),
            claimId: claim.Id,
            sourceId: source.Id,
            locator: request.Locator,
            assessment: request.Assessment,
            recordedAt: DateTimeOffset.UtcNow,
            excerpt: request.Excerpt);

        await evidenceRepository.AddAsync(
            evidence,
            cancellationToken);

        return evidence;
    }
}

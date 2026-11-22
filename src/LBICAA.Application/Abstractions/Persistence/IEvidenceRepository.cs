using EvidenceEntity = LBICAA.Domain.Evidence.Evidence;

namespace LBICAA.Application.Abstractions.Persistence;

public interface IEvidenceRepository
{
    Task AddAsync(
        EvidenceEntity evidence,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvidenceEntity>> GetByClaimIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);
}

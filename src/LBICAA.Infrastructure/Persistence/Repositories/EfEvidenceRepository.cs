using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Domain.Evidence;
using LBICAA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LBICAA.Infrastructure.Persistence.Repositories;

public sealed class EfEvidenceRepository(
    LbicaaDbContext dbContext) : IEvidenceRepository
{
    public async Task AddAsync(
        Evidence evidence,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Evidence.AddAsync(evidence, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Evidence>> GetByClaimIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Evidence
            .Where(x => x.ClaimId == claimId)
            .OrderBy(x => x.RecordedAt)
            .ToListAsync(cancellationToken);
    }
}

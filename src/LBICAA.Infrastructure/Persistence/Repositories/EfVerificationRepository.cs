using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Domain.Verification;
using LBICAA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LBICAA.Infrastructure.Persistence.Repositories;

public sealed class EfVerificationRepository(
    LbicaaDbContext dbContext) : IVerificationRepository
{
    public async Task AddAsync(
        VerificationRecord record,
        CancellationToken cancellationToken = default)
    {
        await dbContext.VerificationRecords.AddAsync(record, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VerificationRecord>> GetByClaimIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.VerificationRecords
            .Where(x => x.ClaimId == claimId)
            .OrderBy(x => x.VerifiedAt)
            .ToListAsync(cancellationToken);
    }
}

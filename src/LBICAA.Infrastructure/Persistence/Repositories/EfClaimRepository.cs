using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Domain.Claims;
using LBICAA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LBICAA.Infrastructure.Persistence.Repositories;

public sealed class EfClaimRepository(
    LbicaaDbContext dbContext) : IClaimRepository
{
    public async Task AddAsync(
        Claim claim,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Claims.AddAsync(claim, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Claim?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Claims
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

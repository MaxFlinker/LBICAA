using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Domain.Sources;
using LBICAA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LBICAA.Infrastructure.Persistence.Repositories;

public sealed class EfSourceRepository(
    LbicaaDbContext dbContext) : ISourceRepository
{
    public async Task AddAsync(
        Source source,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Sources.AddAsync(source, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Source?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Sources
            .SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}

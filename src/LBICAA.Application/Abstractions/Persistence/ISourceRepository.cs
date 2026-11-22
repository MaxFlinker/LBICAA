using LBICAA.Domain.Sources;

namespace LBICAA.Application.Abstractions.Persistence;

public interface ISourceRepository
{
    Task<Source?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Source source,
        CancellationToken cancellationToken = default);
}

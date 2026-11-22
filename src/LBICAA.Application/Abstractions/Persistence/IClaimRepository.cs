using LBICAA.Domain.Claims;

namespace LBICAA.Application.Abstractions.Persistence;

public interface IClaimRepository
{
    Task AddAsync(
        Claim claim,
        CancellationToken cancellationToken = default);

    Task<Claim?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}

using LBICAA.Domain.Verification;

namespace LBICAA.Application.Abstractions.Persistence;

public interface IVerificationRepository
{
    Task AddAsync(
        VerificationRecord record,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VerificationRecord>> GetByClaimIdAsync(
        Guid claimId,
        CancellationToken cancellationToken = default);
}

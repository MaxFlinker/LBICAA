using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Domain.Claims;

namespace LBICAA.Application.Claims.CreateClaim;

public sealed class CreateClaimHandler(
    IClaimRepository claimRepository)
{
    public async Task<Claim> HandleAsync(
        CreateClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claim = new Claim(
            id: Guid.NewGuid(),
            subjectId: request.SubjectId,
            predicate: request.Predicate,
            @object: request.Object,
            nature: request.Nature,
            confidence: request.Confidence,
            validity: request.Validity,
            recordedAt: DateTimeOffset.UtcNow);

        await claimRepository.AddAsync(claim, cancellationToken);

        return claim;
    }
}

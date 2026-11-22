using LBICAA.Application.Abstractions.Persistence;

namespace LBICAA.Application.Claims.GetClaimDetails;

public sealed class GetClaimDetailsHandler(
    IClaimRepository claimRepository,
    IEvidenceRepository evidenceRepository,
    IVerificationRepository verificationRepository)
{
    public async Task<ClaimDetails?> HandleAsync(
        GetClaimDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claim = await claimRepository.GetByIdAsync(
            request.ClaimId,
            cancellationToken);

        if (claim is null)
        {
            return null;
        }

        var evidence = await evidenceRepository.GetByClaimIdAsync(
            request.ClaimId,
            cancellationToken);

        var verificationHistory =
            await verificationRepository.GetByClaimIdAsync(
                request.ClaimId,
                cancellationToken);

        return new ClaimDetails(
            claim,
            evidence,
            verificationHistory);
    }
}

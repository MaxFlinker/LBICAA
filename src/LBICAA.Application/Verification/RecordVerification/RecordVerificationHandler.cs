using LBICAA.Application.Abstractions.Persistence;
using VerificationRecordEntity = LBICAA.Domain.Verification.VerificationRecord;

namespace LBICAA.Application.Verification.RecordVerification;

public sealed class RecordVerificationHandler(
    IClaimRepository claimRepository,
    IVerificationRepository verificationRepository)
{
    public async Task<VerificationRecordEntity> HandleAsync(
        RecordVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var claim = await claimRepository.GetByIdAsync(
            request.ClaimId,
            cancellationToken);

        if (claim is null)
        {
            throw new KeyNotFoundException(
                $"Claim '{request.ClaimId}' was not found.");
        }

        var record = new VerificationRecordEntity(
            id: Guid.NewGuid(),
            claimId: claim.Id,
            method: request.Method,
            outcome: request.Outcome,
            verifierReference: request.VerifierReference,
            verifiedAt: DateTimeOffset.UtcNow,
            notes: request.Notes);

        await verificationRepository.AddAsync(
            record,
            cancellationToken);

        return record;
    }
}

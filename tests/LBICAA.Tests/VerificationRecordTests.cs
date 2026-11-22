using LBICAA.Domain.Verification;

namespace LBICAA.Tests;

public class VerificationRecordTests
{
    [Fact]
    public void EmptyVerifierReference_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            new VerificationRecord(
                Guid.NewGuid(),
                Guid.NewGuid(),
                VerificationMethod.ManualReview,
                VerificationOutcome.Confirmed,
                "",
                DateTimeOffset.UtcNow));
    }

    [Fact]
    public void MultipleVerificationRecordsForSameClaim_AreAccepted()
    {
        var claimId = Guid.NewGuid();

        var first = new VerificationRecord(
            Guid.NewGuid(),
            claimId,
            VerificationMethod.ManualReview,
            VerificationOutcome.Confirmed,
            "reviewer:001",
            DateTimeOffset.UtcNow);

        var second = new VerificationRecord(
            Guid.NewGuid(),
            claimId,
            VerificationMethod.CrossSourceComparison,
            VerificationOutcome.PartiallyConfirmed,
            "system:LBICAA",
            DateTimeOffset.UtcNow);

        Assert.Equal(claimId, first.ClaimId);
        Assert.Equal(claimId, second.ClaimId);
        Assert.NotEqual(first.Id, second.Id);
    }
}

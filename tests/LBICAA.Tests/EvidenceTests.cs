using LBICAA.Domain.Evidence;

namespace LBICAA.Tests;

public class EvidenceTests
{
    private static Evidence CreateEvidence(
        Guid? claimId = null,
        string locator = "Page 12",
        EvidenceAssessment assessment = EvidenceAssessment.Supports)
    {
        return new Evidence(
            Guid.NewGuid(),
            claimId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            locator,
            assessment,
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void EmptyClaimId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateEvidence(claimId: Guid.Empty));
    }

    [Fact]
    public void EmptyLocator_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateEvidence(locator: ""));
    }

    [Fact]
    public void ContradictingEvidence_IsAccepted()
    {
        var evidence = CreateEvidence(
            assessment: EvidenceAssessment.Contradicts);

        Assert.Equal(
            EvidenceAssessment.Contradicts,
            evidence.Assessment);
    }
}

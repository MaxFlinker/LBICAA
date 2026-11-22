using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Application.Claims.GetClaimDetails;
using LBICAA.Domain.Claims;
using LBICAA.Domain.Evidence;
using LBICAA.Domain.Verification;

namespace LBICAA.Tests;

public sealed class GetClaimDetailsHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingClaim_ReturnsClaimAndHistory()
    {
        var claim = CreateClaim();
        var evidence = new Evidence(
            Guid.NewGuid(),
            claim.Id,
            Guid.NewGuid(),
            "https://example.org/document",
            EvidenceAssessment.Supports,
            DateTimeOffset.UtcNow);

        var verification = new VerificationRecord(
            Guid.NewGuid(),
            claim.Id,
            VerificationMethod.ManualReview,
            VerificationOutcome.Confirmed,
            "reviewer:example",
            DateTimeOffset.UtcNow);

        var claims = new FakeClaimRepository { Claim = claim };
        var evidenceRepository = new FakeEvidenceRepository
        {
            Records = [evidence]
        };
        var verificationRepository = new FakeVerificationRepository
        {
            Records = [verification]
        };

        var handler = new GetClaimDetailsHandler(
            claims,
            evidenceRepository,
            verificationRepository);

        var result = await handler.HandleAsync(
            new GetClaimDetailsRequest(claim.Id));

        Assert.NotNull(result);
        Assert.Equal(claim, result.Claim);
        Assert.Single(result.Evidence);
        Assert.Equal(evidence, result.Evidence[0]);
        Assert.Single(result.VerificationHistory);
        Assert.Equal(verification, result.VerificationHistory[0]);
    }

    [Fact]
    public async Task HandleAsync_MissingClaim_ReturnsNullWithoutLoadingHistory()
    {
        var claims = new FakeClaimRepository();
        var evidenceRepository = new FakeEvidenceRepository();
        var verificationRepository = new FakeVerificationRepository();

        var handler = new GetClaimDetailsHandler(
            claims,
            evidenceRepository,
            verificationRepository);

        var result = await handler.HandleAsync(
            new GetClaimDetailsRequest(Guid.NewGuid()));

        Assert.Null(result);
        Assert.False(evidenceRepository.WasQueried);
        Assert.False(verificationRepository.WasQueried);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationToken()
    {
        var claim = CreateClaim();
        var claims = new FakeClaimRepository { Claim = claim };
        var evidenceRepository = new FakeEvidenceRepository();
        var verificationRepository = new FakeVerificationRepository();

        var handler = new GetClaimDetailsHandler(
            claims,
            evidenceRepository,
            verificationRepository);

        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        await handler.HandleAsync(
            new GetClaimDetailsRequest(claim.Id),
            token);

        Assert.Equal(token, claims.ReceivedCancellationToken);
        Assert.Equal(token, evidenceRepository.ReceivedCancellationToken);
        Assert.Equal(token, verificationRepository.ReceivedCancellationToken);
    }

    private static Claim CreateClaim()
    {
        return new Claim(
            Guid.NewGuid(),
            "person:example-001",
            "hasOccupation",
            ClaimValue.Literal("Software Developer"),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(null, null),
            DateTimeOffset.UtcNow);
    }

    private sealed class FakeClaimRepository : IClaimRepository
    {
        public Claim? Claim { get; init; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            Claim claim,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Claim?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(Claim?.Id == id ? Claim : null);
        }
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public IReadOnlyList<Evidence> Records { get; init; } = [];

        public bool WasQueried { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            Evidence evidence,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<Evidence>> GetByClaimIdAsync(
            Guid claimId,
            CancellationToken cancellationToken = default)
        {
            WasQueried = true;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(Records);
        }
    }

    private sealed class FakeVerificationRepository : IVerificationRepository
    {
        public IReadOnlyList<VerificationRecord> Records { get; init; } = [];

        public bool WasQueried { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            VerificationRecord record,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<VerificationRecord>> GetByClaimIdAsync(
            Guid claimId,
            CancellationToken cancellationToken = default)
        {
            WasQueried = true;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(Records);
        }
    }
}

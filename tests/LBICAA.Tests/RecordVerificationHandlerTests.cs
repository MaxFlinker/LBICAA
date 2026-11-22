using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Application.Verification.RecordVerification;
using LBICAA.Domain.Claims;
using LBICAA.Domain.Verification;
using VerificationRecordEntity = LBICAA.Domain.Verification.VerificationRecord;

namespace LBICAA.Tests;

public sealed class RecordVerificationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingClaim_AppendsVerificationRecord()
    {
        var claim = CreateClaim();
        var repository = new FakeVerificationRepository();
        var handler = new RecordVerificationHandler(
            new FakeClaimRepository(claim),
            repository);

        var request = new RecordVerificationRequest(
            claim.Id,
            VerificationMethod.ManualReview,
            VerificationOutcome.Confirmed,
            "reviewer:example",
            "Evidence independently reviewed.");

        var result = await handler.HandleAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(claim.Id, result.ClaimId);
        Assert.Equal(request.Method, result.Method);
        Assert.Equal(request.Outcome, result.Outcome);
        Assert.Equal(request.VerifierReference, result.VerifierReference);
        Assert.Equal(request.Notes, result.Notes);
        Assert.Equal(TimeSpan.Zero, result.VerifiedAt.Offset);

        Assert.Single(repository.Records);
        Assert.Equal(result, repository.Records[0]);
    }

    [Fact]
    public async Task HandleAsync_PreviousVerificationExists_PreservesHistory()
    {
        var claim = CreateClaim();

        var previous = new VerificationRecordEntity(
            Guid.NewGuid(),
            claim.Id,
            VerificationMethod.ManualReview,
            VerificationOutcome.PartiallyConfirmed,
            "reviewer:previous",
            DateTimeOffset.UtcNow.AddDays(-1),
            "Earlier assessment.");

        var repository = new FakeVerificationRepository();
        repository.Records.Add(previous);

        var handler = new RecordVerificationHandler(
            new FakeClaimRepository(claim),
            repository);

        var request = new RecordVerificationRequest(
            claim.Id,
            VerificationMethod.CrossSourceComparison,
            VerificationOutcome.Confirmed,
            "reviewer:second");

        var result = await handler.HandleAsync(request);

        Assert.Equal(2, repository.Records.Count);
        Assert.Equal(previous, repository.Records[0]);
        Assert.Equal(result, repository.Records[1]);
        Assert.NotEqual(previous.Id, result.Id);
    }

    [Fact]
    public async Task HandleAsync_MissingClaim_ThrowsAndDoesNotSave()
    {
        var repository = new FakeVerificationRepository();

        var handler = new RecordVerificationHandler(
            new FakeClaimRepository(null),
            repository);

        var request = new RecordVerificationRequest(
            Guid.NewGuid(),
            VerificationMethod.ManualReview,
            VerificationOutcome.Rejected,
            "reviewer:example");

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(request));

        Assert.Empty(repository.Records);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationToken()
    {
        var claim = CreateClaim();
        var claimRepository = new FakeClaimRepository(claim);
        var verificationRepository = new FakeVerificationRepository();

        var handler = new RecordVerificationHandler(
            claimRepository,
            verificationRepository);

        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        var request = new RecordVerificationRequest(
            claim.Id,
            VerificationMethod.LegalReview,
            VerificationOutcome.Inconclusive,
            "reviewer:legal");

        await handler.HandleAsync(request, token);

        Assert.Equal(token, claimRepository.ReceivedCancellationToken);
        Assert.Equal(token, verificationRepository.ReceivedCancellationToken);
    }

    private static Claim CreateClaim() =>
        new(
            Guid.NewGuid(),
            "person:example-001",
            "hasOccupation",
            ClaimValue.Literal("Software Developer"),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(null, null),
            DateTimeOffset.UtcNow);

    private sealed class FakeClaimRepository(Claim? record) : IClaimRepository
    {
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
            return Task.FromResult(record?.Id == id ? record : null);
        }
    }

    private sealed class FakeVerificationRepository
        : IVerificationRepository
    {
        public List<VerificationRecordEntity> Records { get; } = [];

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            VerificationRecordEntity record,
            CancellationToken cancellationToken = default)
        {
            Records.Add(record);
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<VerificationRecordEntity>> GetByClaimIdAsync(
            Guid claimId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<VerificationRecordEntity> result = Records
                .Where(x => x.ClaimId == claimId)
                .ToList();

            return Task.FromResult(result);
        }
    }
}

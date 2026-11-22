using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Application.Claims.CreateClaim;
using LBICAA.Domain.Claims;

namespace LBICAA.Tests;

public sealed class CreateClaimHandlerTests
{
    [Fact]
    public async Task HandleAsync_ValidRequest_PersistsAndReturnsClaim()
    {
        var repository = new FakeClaimRepository();
        var handler = new CreateClaimHandler(repository);

        var validity = new TemporalValidity(
            new DateOnly(2020, 1, 1),
            null);

        var request = new CreateClaimRequest(
            SubjectId: "person:example-001",
            Predicate: "hasOccupation",
            Object: ClaimValue.Literal("Software Developer"),
            Nature: ClaimNature.Documented,
            Confidence: ConfidenceLevel.High,
            Validity: validity);

        var result = await handler.HandleAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(request.SubjectId, result.SubjectId);
        Assert.Equal(request.Predicate, result.Predicate);
        Assert.Equal(request.Object, result.Object);
        Assert.Equal(request.Nature, result.Nature);
        Assert.Equal(request.Confidence, result.Confidence);
        Assert.Equal(request.Validity, result.Validity);

        Assert.NotNull(repository.SavedClaim);
        Assert.Equal(result, repository.SavedClaim);
        Assert.Equal(TimeSpan.Zero, result.RecordedAt.Offset);
    }

    [Fact]
    public async Task HandleAsync_PassesCancellationTokenToRepository()
    {
        var repository = new FakeClaimRepository();
        var handler = new CreateClaimHandler(repository);

        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        var request = new CreateClaimRequest(
            SubjectId: "person:example-002",
            Predicate: "hasName",
            Object: ClaimValue.Literal("Example"),
            Nature: ClaimNature.Documented,
            Confidence: ConfidenceLevel.Medium,
            Validity: new TemporalValidity(null, null));

        await handler.HandleAsync(request, token);

        Assert.Equal(token, repository.ReceivedCancellationToken);
    }

    [Fact]
    public async Task HandleAsync_NullRequest_ThrowsArgumentNullException()
    {
        var repository = new FakeClaimRepository();
        var handler = new CreateClaimHandler(repository);

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.HandleAsync(null!));
    }

    private sealed class FakeClaimRepository : IClaimRepository
    {
        public Claim? SavedClaim { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            Claim claim,
            CancellationToken cancellationToken = default)
        {
            SavedClaim = claim;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<Claim?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Claim?>(null);
        }
    }
}

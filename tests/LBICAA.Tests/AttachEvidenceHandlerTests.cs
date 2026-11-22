using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Application.Evidence.AttachEvidence;
using LBICAA.Domain.Claims;
using LBICAA.Domain.Evidence;
using EvidenceEntity = LBICAA.Domain.Evidence.Evidence;
using LBICAA.Domain.Sources;

namespace LBICAA.Tests;

public sealed class AttachEvidenceHandlerTests
{
    [Fact]
    public async Task HandleAsync_ExistingClaimAndSource_SavesEvidence()
    {
        var claim = CreateClaim();
        var source = CreateSource();

        var evidenceRepository = new FakeEvidenceRepository();

        var handler = new AttachEvidenceHandler(
            new FakeClaimRepository(claim),
            new FakeSourceRepository(source),
            evidenceRepository);

        var request = new AttachEvidenceRequest(
            claim.Id,
            source.Id,
            "https://example.org/document#page=4",
            EvidenceAssessment.Supports,
            "Relevant passage on page 4.");

        var result = await handler.HandleAsync(request);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(claim.Id, result.ClaimId);
        Assert.Equal(source.Id, result.SourceId);
        Assert.Equal(request.Locator, result.Locator);
        Assert.Equal(request.Assessment, result.Assessment);
        Assert.Equal(request.Excerpt, result.Excerpt);
        Assert.Equal(TimeSpan.Zero, result.RecordedAt.Offset);

        Assert.Equal(result, evidenceRepository.SavedEvidence);
    }

    [Fact]
    public async Task HandleAsync_MissingClaim_ThrowsAndDoesNotSave()
    {
        var evidenceRepository = new FakeEvidenceRepository();

        var handler = new AttachEvidenceHandler(
            new FakeClaimRepository(null),
            new FakeSourceRepository(CreateSource()),
            evidenceRepository);

        var request = new AttachEvidenceRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.org/document",
            EvidenceAssessment.Supports);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(request));

        Assert.Null(evidenceRepository.SavedEvidence);
    }

    [Fact]
    public async Task HandleAsync_MissingSource_ThrowsAndDoesNotSave()
    {
        var evidenceRepository = new FakeEvidenceRepository();

        var handler = new AttachEvidenceHandler(
            new FakeClaimRepository(CreateClaim()),
            new FakeSourceRepository(null),
            evidenceRepository);

        var request = new AttachEvidenceRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.org/document",
            EvidenceAssessment.Contradicts);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => handler.HandleAsync(request));

        Assert.Null(evidenceRepository.SavedEvidence);
    }

    [Fact]
    public async Task HandleAsync_PropagatesCancellationToken()
    {
        var claimRepository = new FakeClaimRepository(CreateClaim());
        var sourceRepository = new FakeSourceRepository(CreateSource());
        var evidenceRepository = new FakeEvidenceRepository();

        var handler = new AttachEvidenceHandler(
            claimRepository,
            sourceRepository,
            evidenceRepository);

        using var cancellationSource = new CancellationTokenSource();
        var token = cancellationSource.Token;

        var request = new AttachEvidenceRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "https://example.org/document",
            EvidenceAssessment.Contextual);

        // Use the actual IDs of the records held by the repositories.
        request = request with
        {
            ClaimId = claimRepository.Record!.Id,
            SourceId = sourceRepository.Record!.Id
        };

        await handler.HandleAsync(request, token);

        Assert.Equal(token, claimRepository.ReceivedCancellationToken);
        Assert.Equal(token, sourceRepository.ReceivedCancellationToken);
        Assert.Equal(token, evidenceRepository.ReceivedCancellationToken);
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

    private static Source CreateSource() =>
        new(
            Guid.NewGuid(),
            "Example document",
            "Example publisher",
            new Uri("https://example.org/document"),
            SourceType.OfficialDocument,
            DateTimeOffset.UtcNow,
            LicenceStatus.Known);

    private sealed class FakeClaimRepository(Claim? record) : IClaimRepository
    {
        public Claim? Record => record;

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

    private sealed class FakeSourceRepository(Source? record) : ISourceRepository
    {
        public Source? Record => record;

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            Source source,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<Source?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(record?.Id == id ? record : null);
        }
    }

    private sealed class FakeEvidenceRepository : IEvidenceRepository
    {
        public EvidenceEntity? SavedEvidence { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task AddAsync(
            Evidence evidence,
            CancellationToken cancellationToken = default)
        {
            SavedEvidence = evidence;
            ReceivedCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<EvidenceEntity>> GetByClaimIdAsync(
            Guid claimId,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EvidenceEntity>>([]);
    }
}


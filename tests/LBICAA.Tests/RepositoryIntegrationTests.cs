using LBICAA.Domain.Claims;
using LBICAA.Domain.Evidence;
using LBICAA.Domain.Sources;
using LBICAA.Domain.Verification;
using LBICAA.Infrastructure.Persistence;
using LBICAA.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LBICAA.Tests;

public class RepositoryIntegrationTests
{
    private static DbContextOptions<LbicaaDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<LbicaaDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static Claim CreateClaim(
        Guid? id = null,
        DateTimeOffset? recordedAt = null)
    {
        return new Claim(
            id ?? Guid.NewGuid(),
            "entity:subject-001",
            "has-status",
            ClaimValue.Literal("active"),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(null, null),
            recordedAt ?? DateTimeOffset.UtcNow);
    }

    private static Source CreateSource(Guid? id = null)
    {
        return new Source(
            id ?? Guid.NewGuid(),
            "Test source",
            "LBICAA Tests",
            new Uri("https://example.org/source"),
            SourceType.OfficialDocument,
            DateTimeOffset.UtcNow,
            LicenceStatus.Known,
            publishedOn: new DateOnly(2026, 1, 1),
            licenceReference: "Test licence",
            contentHash: "test-hash");
    }

    private static Evidence CreateEvidence(
        Guid claimId,
        Guid sourceId,
        DateTimeOffset recordedAt)
    {
        return new Evidence(
            Guid.NewGuid(),
            claimId,
            sourceId,
            "Page 1, paragraph 2",
            EvidenceAssessment.Supports,
            recordedAt,
            "Test excerpt");
    }

    private static VerificationRecord CreateVerification(
        Guid claimId,
        DateTimeOffset verifiedAt)
    {
        return new VerificationRecord(
            Guid.NewGuid(),
            claimId,
            VerificationMethod.ManualReview,
            VerificationOutcome.Confirmed,
            "reviewer:test",
            verifiedAt,
            "Test verification");
    }

    [Fact]
    public async Task ClaimRepository_AddsAndRetrievesClaim()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfClaimRepository(dbContext);
        var claim = CreateClaim();

        await repository.AddAsync(claim);

        var retrieved = await repository.GetByIdAsync(claim.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(claim.Id, retrieved.Id);
        Assert.Equal(claim.SubjectId, retrieved.SubjectId);
        Assert.Equal(claim.Predicate, retrieved.Predicate);
        Assert.Equal(claim.Object.Value, retrieved.Object.Value);
        Assert.Equal(claim.Nature, retrieved.Nature);
        Assert.Equal(claim.Confidence, retrieved.Confidence);
    }

    [Fact]
    public async Task ClaimRepository_ReturnsNullForUnknownId()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfClaimRepository(dbContext);

        var retrieved = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task SourceRepository_AddsAndRetrievesSource()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfSourceRepository(dbContext);
        var source = CreateSource();

        await repository.AddAsync(source);

        var retrieved = await repository.GetByIdAsync(source.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(source.Id, retrieved.Id);
        Assert.Equal(source.Title, retrieved.Title);
        Assert.Equal(source.Publisher, retrieved.Publisher);
        Assert.Equal(source.Uri, retrieved.Uri);
        Assert.Equal(source.Type, retrieved.Type);
        Assert.Equal(source.LicenceStatus, retrieved.LicenceStatus);
        Assert.Equal(source.ContentHash, retrieved.ContentHash);
    }

    [Fact]
    public async Task SourceRepository_ReturnsNullForUnknownId()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfSourceRepository(dbContext);

        var retrieved = await repository.GetByIdAsync(Guid.NewGuid());

        Assert.Null(retrieved);
    }

    [Fact]
    public async Task EvidenceRepository_AddsAndRetrievesEvidenceInChronologicalOrder()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var claim = CreateClaim();
        var source = CreateSource();

        await dbContext.Claims.AddAsync(claim);
        await dbContext.Sources.AddAsync(source);
        await dbContext.SaveChangesAsync();

        var repository = new EfEvidenceRepository(dbContext);

        var earlier = CreateEvidence(
            claim.Id,
            source.Id,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        var later = CreateEvidence(
            claim.Id,
            source.Id,
            new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero));

        // Deliberately insert in reverse chronological order.
        await repository.AddAsync(later);
        await repository.AddAsync(earlier);

        var results = await repository.GetByClaimIdAsync(claim.Id);

        Assert.Equal(2, results.Count);
        Assert.Equal(
            new[] { earlier.Id, later.Id },
            results.Select(x => x.Id));

        Assert.All(results, item =>
            Assert.Equal(claim.Id, item.ClaimId));
    }

    [Fact]
    public async Task EvidenceRepository_ReturnsEmptyListForClaimWithoutEvidence()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfEvidenceRepository(dbContext);

        var results = await repository.GetByClaimIdAsync(Guid.NewGuid());

        Assert.Empty(results);
    }

    [Fact]
    public async Task VerificationRepository_AddsAndRetrievesRecordsInChronologicalOrder()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var claim = CreateClaim();

        await dbContext.Claims.AddAsync(claim);
        await dbContext.SaveChangesAsync();

        var repository = new EfVerificationRepository(dbContext);

        var earlier = CreateVerification(
            claim.Id,
            new DateTimeOffset(2026, 1, 1, 10, 0, 0, TimeSpan.Zero));

        var later = CreateVerification(
            claim.Id,
            new DateTimeOffset(2026, 1, 2, 10, 0, 0, TimeSpan.Zero));

        // Deliberately insert in reverse chronological order.
        await repository.AddAsync(later);
        await repository.AddAsync(earlier);

        var results = await repository.GetByClaimIdAsync(claim.Id);

        Assert.Equal(2, results.Count);
        Assert.Equal(
            new[] { earlier.Id, later.Id },
            results.Select(x => x.Id));

        Assert.All(results, item =>
            Assert.Equal(claim.Id, item.ClaimId));
    }

    [Fact]
    public async Task VerificationRepository_ReturnsEmptyListForClaimWithoutRecords()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = CreateOptions(connection);

        await using var dbContext = new LbicaaDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var repository = new EfVerificationRepository(dbContext);

        var results = await repository.GetByClaimIdAsync(Guid.NewGuid());

        Assert.Empty(results);
    }
}

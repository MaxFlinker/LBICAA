
using LBICAA.Domain.Claims;
using LBICAA.Domain.Evidence;
using LBICAA.Domain.Sources;
using LBICAA.Domain.Verification;
using LBICAA.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LBICAA.Tests;

public class PersistenceIntegrationTests
{
    private static DbContextOptions<LbicaaDbContext> CreateOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<LbicaaDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    [Fact]
    public void Claim_RoundTripsThroughSqlite()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        var claimId = Guid.NewGuid();
        var recordedAt = DateTimeOffset.UtcNow;
        var validFrom = new DateOnly(2020, 1, 1);
        var validUntil = new DateOnly(2025, 12, 31);

        var claim = new Claim(
            claimId,
            "organisation:hamburg",
            "hasOfficialWebsite",
            ClaimValue.Entity("source:hamburg"),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(validFrom, validUntil),
            recordedAt);

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            context.Database.EnsureCreated();
            context.Claims.Add(claim);
            context.SaveChanges();
        }

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            var stored = context.Claims
                .AsNoTracking()
                .Single(x => x.Id == claimId);

            Assert.Equal("organisation:hamburg", stored.SubjectId);
            Assert.Equal("hasOfficialWebsite", stored.Predicate);

            Assert.Equal(
                ClaimValueKind.EntityReference,
                stored.Object.Kind);

            Assert.Equal("source:hamburg", stored.Object.Value);

            Assert.Equal(ClaimNature.Documented, stored.Nature);
            Assert.Equal(ClaimStatus.Active, stored.Status);
            Assert.Equal(ConfidenceLevel.High, stored.Confidence);

            Assert.Equal(validFrom, stored.Validity.ValidFrom);
            Assert.Equal(validUntil, stored.Validity.ValidUntil);
        }
    }

    [Fact]
    public void Source_RoundTripsThroughSqlite()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        var sourceId = Guid.NewGuid();
        var uri = new Uri("https://www.hamburg.de/");
        var retrievedAt = DateTimeOffset.UtcNow;

        var source = new Source(
            sourceId,
            "Hamburg Organisation Directory",
            "Freie und Hansestadt Hamburg",
            uri,
            SourceType.OfficialWebsite,
            retrievedAt,
            LicenceStatus.Known,
            publishedOn: new DateOnly(2026, 1, 15),
            licenceReference: "Official terms",
            contentHash: "sha256:example");

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            context.Database.EnsureCreated();
            context.Sources.Add(source);
            context.SaveChanges();
        }

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            var stored = context.Sources
                .AsNoTracking()
                .Single(x => x.Id == sourceId);

            Assert.Equal(source.Title, stored.Title);
            Assert.Equal(source.Publisher, stored.Publisher);
            Assert.Equal(uri, stored.Uri);
            Assert.Equal(SourceType.OfficialWebsite, stored.Type);
            Assert.Equal(LicenceStatus.Known, stored.LicenceStatus);
            Assert.Equal(new DateOnly(2026, 1, 15), stored.PublishedOn);
            Assert.Equal("Official terms", stored.LicenceReference);
            Assert.Equal("sha256:example", stored.ContentHash);
        }
    }

    [Fact]
    public void EvidenceAndVerificationRecords_RoundTripThroughSqlite()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        var claimId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var evidenceId = Guid.NewGuid();
        var verificationId = Guid.NewGuid();

        var claim = new Claim(
            claimId,
            "organisation:hamburg",
            "hasOfficialWebsite",
            ClaimValue.Literal("https://www.hamburg.de/"),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(null, null),
            DateTimeOffset.UtcNow);

        var source = new Source(
            sourceId,
            "Hamburg Official Website",
            "Freie und Hansestadt Hamburg",
            new Uri("https://www.hamburg.de/"),
            SourceType.OfficialWebsite,
            DateTimeOffset.UtcNow,
            LicenceStatus.Known);

        var evidence = new Evidence(
            evidenceId,
            claimId,
            sourceId,
            "Page 12",
            EvidenceAssessment.Supports,
            DateTimeOffset.UtcNow,
            "Official website reference.");

        var verification = new VerificationRecord(
            verificationId,
            claimId,
            VerificationMethod.ManualReview,
            VerificationOutcome.Confirmed,
            "reviewer:001",
            DateTimeOffset.UtcNow);

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            context.Database.EnsureCreated();

            context.Claims.Add(claim);
            context.Sources.Add(source);
            context.Evidence.Add(evidence);
            context.VerificationRecords.Add(verification);

            context.SaveChanges();
        }

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            var storedEvidence = context.Evidence
                .AsNoTracking()
                .Single(x => x.Id == evidenceId);

            var storedVerification = context.VerificationRecords
                .AsNoTracking()
                .Single(x => x.Id == verificationId);

            Assert.Equal(claimId, storedEvidence.ClaimId);
            Assert.Equal(sourceId, storedEvidence.SourceId);
            Assert.Equal("Page 12", storedEvidence.Locator);
            Assert.Equal(
                EvidenceAssessment.Supports,
                storedEvidence.Assessment);
            Assert.Equal(
                "Official website reference.",
                storedEvidence.Excerpt);

            Assert.Equal(claimId, storedVerification.ClaimId);
            Assert.Equal(
                VerificationMethod.ManualReview,
                storedVerification.Method);
            Assert.Equal(
                VerificationOutcome.Confirmed,
                storedVerification.Outcome);
            Assert.Equal(
                "reviewer:001",
                storedVerification.VerifierReference);
        }
    }

    [Fact]
    public void Evidence_WithMissingClaim_IsRejectedByForeignKey()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        using var context = new LbicaaDbContext(
            CreateOptions(connection));

        context.Database.EnsureCreated();

        var evidence = new Evidence(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Page 12",
            EvidenceAssessment.Supports,
            DateTimeOffset.UtcNow);

        context.Evidence.Add(evidence);

        Assert.Throws<DbUpdateException>(
            () => context.SaveChanges());
    }

    [Fact]
    public void Evidence_WithMissingSource_IsRejectedByForeignKey()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        using var context = new LbicaaDbContext(
            CreateOptions(connection));

        context.Database.EnsureCreated();

        var claim = new Claim(
            Guid.NewGuid(),
            "organisation:hamburg",
            "hasOfficialWebsite",
            ClaimValue.Literal("https://www.hamburg.de/"),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(null, null),
            DateTimeOffset.UtcNow);

        context.Claims.Add(claim);
        context.SaveChanges();

        var evidence = new Evidence(
            Guid.NewGuid(),
            claim.Id,
            Guid.NewGuid(),
            "Page 12",
            EvidenceAssessment.Supports,
            DateTimeOffset.UtcNow);

        context.Evidence.Add(evidence);

        Assert.Throws<DbUpdateException>(
            () => context.SaveChanges());
    }

    
[Fact]
public void Claim_WithEvidence_CannotBeDeleted()
{
    using var connection = new SqliteConnection(
        "Data Source=:memory:");

    connection.Open();

    var claimId = Guid.NewGuid();

    var claim = new Claim(
        claimId,
        "organisation:hamburg",
        "hasOfficialWebsite",
        ClaimValue.Literal("https://www.hamburg.de/"),
        ClaimNature.Documented,
        ConfidenceLevel.High,
        new TemporalValidity(null, null),
        DateTimeOffset.UtcNow);

    var source = new Source(
        Guid.NewGuid(),
        "Hamburg Official Website",
        "Freie und Hansestadt Hamburg",
        new Uri("https://www.hamburg.de/"),
        SourceType.OfficialWebsite,
        DateTimeOffset.UtcNow,
        LicenceStatus.Known);

    // Seed the claim, source and evidence.
    using (var context = new LbicaaDbContext(
        CreateOptions(connection)))
    {
        context.Database.EnsureCreated();

        context.Claims.Add(claim);
        context.Sources.Add(source);
        context.SaveChanges();

        context.Evidence.Add(new Evidence(
            Guid.NewGuid(),
            claimId,
            source.Id,
            "Page 12",
            EvidenceAssessment.Supports,
            DateTimeOffset.UtcNow));

        context.SaveChanges();
    }

    // Use a fresh context so dependent records are not tracked.
    using (var context = new LbicaaDbContext(
        CreateOptions(connection)))
    {
        var storedClaim = context.Claims.Single(
            x => x.Id == claimId);

        context.Claims.Remove(storedClaim);

        Assert.Throws<DbUpdateException>(
            () => context.SaveChanges());
    }
}


[Fact]
public void Claim_WithVerificationRecord_CannotBeDeleted()
{
    using var connection = new SqliteConnection(
        "Data Source=:memory:");

    connection.Open();

    var claimId = Guid.NewGuid();

    var claim = new Claim(
        claimId,
        "organisation:hamburg",
        "hasOfficialWebsite",
        ClaimValue.Literal("https://www.hamburg.de/"),
        ClaimNature.Documented,
        ConfidenceLevel.High,
        new TemporalValidity(null, null),
        DateTimeOffset.UtcNow);

    // Seed the claim and verification record.
    using (var context = new LbicaaDbContext(
        CreateOptions(connection)))
    {
        context.Database.EnsureCreated();

        context.Claims.Add(claim);
        context.SaveChanges();

        context.VerificationRecords.Add(
            new VerificationRecord(
                Guid.NewGuid(),
                claimId,
                VerificationMethod.ManualReview,
                VerificationOutcome.Confirmed,
                "reviewer:001",
                DateTimeOffset.UtcNow));

        context.SaveChanges();
    }

    // Use a fresh context so the verification record is not tracked.
    using (var context = new LbicaaDbContext(
        CreateOptions(connection)))
    {
        var storedClaim = context.Claims.Single(
            x => x.Id == claimId);

        context.Claims.Remove(storedClaim);

        Assert.Throws<DbUpdateException>(
            () => context.SaveChanges());
    }
}

    [Fact]
    public void LiteralClaimValue_RoundTripsThroughSqlite()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        connection.Open();

        var claimId = Guid.NewGuid();
        const string literal = "Freie und Hansestadt Hamburg";

        var claim = new Claim(
            claimId,
            "organisation:hamburg",
            "officialName",
            ClaimValue.Literal(literal),
            ClaimNature.Documented,
            ConfidenceLevel.High,
            new TemporalValidity(
                new DateOnly(2020, 1, 1),
                null),
            DateTimeOffset.UtcNow);

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            context.Database.EnsureCreated();
            context.Claims.Add(claim);
            context.SaveChanges();
        }

        using (var context = new LbicaaDbContext(
            CreateOptions(connection)))
        {
            var stored = context.Claims
                .AsNoTracking()
                .Single(x => x.Id == claimId);

            Assert.Equal(
                ClaimValueKind.Literal,
                stored.Object.Kind);

            Assert.Equal(literal, stored.Object.Value);
            Assert.Equal(
                new DateOnly(2020, 1, 1),
                stored.Validity.ValidFrom);

            Assert.Null(stored.Validity.ValidUntil);
        }
    }
}
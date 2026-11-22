using System.Text.Json;

using ClaimValue = LBICAA.Domain.Claims.ClaimValue;
using TemporalValidity = LBICAA.Domain.Claims.TemporalValidity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ClaimEntity = LBICAA.Domain.Claims.Claim;
using EvidenceEntity = LBICAA.Domain.Evidence.Evidence;
using SourceEntity = LBICAA.Domain.Sources.Source;
using VerificationEntity = LBICAA.Domain.Verification.VerificationRecord;

namespace LBICAA.Infrastructure.Persistence;

public sealed class LbicaaDbContext(
    DbContextOptions<LbicaaDbContext> options) : DbContext(options)
{
    public DbSet<ClaimEntity> Claims => Set<ClaimEntity>();
    public DbSet<EvidenceEntity> Evidence => Set<EvidenceEntity>();
    public DbSet<SourceEntity> Sources => Set<SourceEntity>();
    public DbSet<VerificationEntity> VerificationRecords =>
        Set<VerificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ConfigureClaims(modelBuilder);
        ConfigureEvidence(modelBuilder);
        ConfigureSources(modelBuilder);
        ConfigureVerificationRecords(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    private static void ConfigureClaims(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<ClaimEntity>();

        entity.ToTable("Claims");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.SubjectId)
            .HasMaxLength(256)
            .IsRequired();

        entity.Property(x => x.Predicate)
            .HasMaxLength(256)
            .IsRequired();

        entity.Property(x => x.Nature)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.Confidence)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.RecordedAt)
            .IsRequired();

        entity.Property(x => x.Object)
    .HasConversion(
        value => SerializeClaimValue(value),
        value => DeserializeClaimValue(value))
    .HasColumnName("Object")
    .HasColumnType("TEXT")
    .IsRequired();


        entity.Property(x => x.Validity)
    .HasConversion(
        value => SerializeTemporalValidity(value),
        value => DeserializeTemporalValidity(value))
    .HasColumnName("Validity")
    .HasColumnType("TEXT")
    .IsRequired();


        entity.HasIndex(x => x.SubjectId);
        entity.HasIndex(x => x.Predicate);
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => x.RecordedAt);
    }

    private static void ConfigureEvidence(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<EvidenceEntity>();

        entity.ToTable("Evidence");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Locator)
            .HasMaxLength(2048)
            .IsRequired();

        entity.Property(x => x.Excerpt);

        entity.Property(x => x.Assessment)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.RecordedAt)
            .IsRequired();

        entity.HasOne<ClaimEntity>()
            .WithMany()
            .HasForeignKey(x => x.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne<SourceEntity>()
            .WithMany()
            .HasForeignKey(x => x.SourceId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => x.ClaimId);
        entity.HasIndex(x => x.SourceId);
        entity.HasIndex(x => x.Assessment);
    }

    private static void ConfigureSources(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<SourceEntity>();

        entity.ToTable("Sources");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Title)
            .HasMaxLength(1000)
            .IsRequired();

        entity.Property(x => x.Publisher)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(x => x.Uri)
            .HasConversion(
                uri => uri.ToString(),
                value => new Uri(value, UriKind.Absolute))
            .HasMaxLength(2048)
            .IsRequired();

        entity.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.LicenceStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.LicenceReference)
            .HasMaxLength(1000);

        entity.Property(x => x.ContentHash)
            .HasMaxLength(256);

        entity.Property(x => x.RetrievedAt)
            .IsRequired();

	entity.Property(x => x.PublishedOn)
    	    .IsRequired(false);

        entity.HasIndex(x => x.Uri);
        entity.HasIndex(x => x.ContentHash);
        entity.HasIndex(x => x.Type);
    }

    private static void ConfigureVerificationRecords(
        ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VerificationEntity>();

        entity.ToTable("VerificationRecords");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.Outcome)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        entity.Property(x => x.VerifierReference)
            .HasMaxLength(500)
            .IsRequired();

        entity.Property(x => x.Notes);

        entity.Property(x => x.VerifiedAt)
            .IsRequired();

        entity.HasOne<ClaimEntity>()
            .WithMany()
            .HasForeignKey(x => x.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(x => x.ClaimId);
        entity.HasIndex(x => x.VerifiedAt);
    }
private static string SerializeClaimValue(ClaimValue value)
{
    return JsonSerializer.Serialize(
        new ClaimValueData(value.Kind, value.Value));
}

private static ClaimValue DeserializeClaimValue(string json)
{
    var data = JsonSerializer.Deserialize<ClaimValueData>(json)
        ?? throw new InvalidOperationException(
            "Could not deserialize ClaimValue.");

    return data.Kind switch
    {
        LBICAA.Domain.Claims.ClaimValueKind.EntityReference =>
            ClaimValue.Entity(data.Value),

        LBICAA.Domain.Claims.ClaimValueKind.Literal =>
            ClaimValue.Literal(data.Value),

        _ => throw new InvalidOperationException(
            $"Unknown claim value kind: {data.Kind}")
    };
}

private static string SerializeTemporalValidity(
    TemporalValidity value)
{
    return JsonSerializer.Serialize(
        new TemporalValidityData(value.ValidFrom, value.ValidUntil));
}

private static TemporalValidity DeserializeTemporalValidity(string json)
{
    var data = JsonSerializer.Deserialize<TemporalValidityData>(json)
        ?? throw new InvalidOperationException(
            "Could not deserialize TemporalValidity.");

    return new TemporalValidity(data.ValidFrom, data.ValidUntil);
}

private sealed record ClaimValueData(
    LBICAA.Domain.Claims.ClaimValueKind Kind,
    string Value);

private sealed record TemporalValidityData(
    DateOnly? ValidFrom,
    DateOnly? ValidUntil);
}

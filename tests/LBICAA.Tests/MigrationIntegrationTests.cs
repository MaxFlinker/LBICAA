using LBICAA.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using LBICAA.Domain.Claims;
using LBICAA.Infrastructure.Persistence.Repositories;
namespace LBICAA.Tests;

public class MigrationIntegrationTests
{
    [Fact]
    public async Task Database_Migrations_CreateExpectedSchema()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LbicaaDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LbicaaDbContext(options);

        // Execute the actual EF Core migration files.
        await dbContext.Database.MigrateAsync();

        var availableMigrations =
            dbContext.Database.GetMigrations().ToArray();

        var appliedMigrations =
            (await dbContext.Database.GetAppliedMigrationsAsync()).ToArray();

        Assert.NotEmpty(availableMigrations);
        Assert.Equal(availableMigrations, appliedMigrations);

        var expectedTables = new[]
        {
            "Claims",
            "Sources",
            "Evidence",
            "VerificationRecords",
            "__EFMigrationsHistory"
        };

        foreach (var table in expectedTables)
        {
            var exists = await TableExistsAsync(
                connection,
                table);

            Assert.True(
                exists,
                $"Expected database table '{table}' was not created.");
        }
        
    }

    private static async Task<bool> TableExistsAsync(
        SqliteConnection connection,
        string tableName)
    {
        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = $tableName;
            """;

        command.Parameters.AddWithValue("$tableName", tableName);

        var result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) > 0;
    }
[Fact]
public async Task MigratedDatabase_SupportsRepositoryOperations()
{
    using var connection = new SqliteConnection(
        "Data Source=:memory:");

    await connection.OpenAsync();

    var options = new DbContextOptionsBuilder<LbicaaDbContext>()
        .UseSqlite(connection)
        .Options;

    await using var dbContext = new LbicaaDbContext(options);

    await dbContext.Database.MigrateAsync();

    var repository = new EfClaimRepository(dbContext);

    var claim = new Claim(
        Guid.NewGuid(),
        "entity:subject-001",
        "has-status",
        ClaimValue.Literal("active"),
        ClaimNature.Documented,
        ConfidenceLevel.High,
        new TemporalValidity(null, null),
        DateTimeOffset.UtcNow);

    await repository.AddAsync(claim);

    var retrieved = await repository.GetByIdAsync(claim.Id);

    Assert.NotNull(retrieved);
    Assert.Equal(claim.Id, retrieved.Id);
    Assert.Equal(claim.SubjectId, retrieved.SubjectId);
    Assert.Equal(claim.Object.Value, retrieved.Object.Value);
}
}
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LBICAA.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class _20261122143236_InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Claims",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                SubjectId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Predicate = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                Object = table.Column<string>(type: "TEXT", nullable: false),
                Nature = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Confidence = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Validity = table.Column<string>(type: "TEXT", nullable: false),
                RecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Claims", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Sources",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Publisher = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Uri = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                Type = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                PublishedOn = table.Column<DateOnly>(type: "TEXT", nullable: true),
                RetrievedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                LicenceStatus = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                LicenceReference = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                ContentHash = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sources", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "VerificationRecords",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClaimId = table.Column<Guid>(type: "TEXT", nullable: false),
                Method = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Outcome = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                VerifierReference = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                Notes = table.Column<string>(type: "TEXT", nullable: true),
                VerifiedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_VerificationRecords", x => x.Id);
                table.ForeignKey(
                    name: "FK_VerificationRecords_Claims_ClaimId",
                    column: x => x.ClaimId,
                    principalTable: "Claims",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "Evidence",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                ClaimId = table.Column<Guid>(type: "TEXT", nullable: false),
                SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                Locator = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                Excerpt = table.Column<string>(type: "TEXT", nullable: true),
                Assessment = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                RecordedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Evidence", x => x.Id);
                table.ForeignKey(
                    name: "FK_Evidence_Claims_ClaimId",
                    column: x => x.ClaimId,
                    principalTable: "Claims",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_Evidence_Sources_SourceId",
                    column: x => x.SourceId,
                    principalTable: "Sources",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Claims_Predicate",
            table: "Claims",
            column: "Predicate");

        migrationBuilder.CreateIndex(
            name: "IX_Claims_RecordedAt",
            table: "Claims",
            column: "RecordedAt");

        migrationBuilder.CreateIndex(
            name: "IX_Claims_Status",
            table: "Claims",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_Claims_SubjectId",
            table: "Claims",
            column: "SubjectId");

        migrationBuilder.CreateIndex(
            name: "IX_Evidence_Assessment",
            table: "Evidence",
            column: "Assessment");

        migrationBuilder.CreateIndex(
            name: "IX_Evidence_ClaimId",
            table: "Evidence",
            column: "ClaimId");

        migrationBuilder.CreateIndex(
            name: "IX_Evidence_SourceId",
            table: "Evidence",
            column: "SourceId");

        migrationBuilder.CreateIndex(
            name: "IX_Sources_ContentHash",
            table: "Sources",
            column: "ContentHash");

        migrationBuilder.CreateIndex(
            name: "IX_Sources_Type",
            table: "Sources",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Sources_Uri",
            table: "Sources",
            column: "Uri");

        migrationBuilder.CreateIndex(
            name: "IX_VerificationRecords_ClaimId",
            table: "VerificationRecords",
            column: "ClaimId");

        migrationBuilder.CreateIndex(
            name: "IX_VerificationRecords_VerifiedAt",
            table: "VerificationRecords",
            column: "VerifiedAt");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Evidence");

        migrationBuilder.DropTable(
            name: "VerificationRecords");

        migrationBuilder.DropTable(
            name: "Sources");

        migrationBuilder.DropTable(
            name: "Claims");
    }
}

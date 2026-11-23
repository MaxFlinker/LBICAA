using System.Text.Json;
using LBICAA.Crawler;

namespace LBICAA.Tests;

public class XlsxStructureValidatorTests
{
    private static XlsxTable1Extractor.ExtractionResult ExtractFixture()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        var directory = Path.Combine(
            Path.GetTempPath(),
            "LBICAA.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        try
        {
            var outputPath = Path.Combine(directory, "table1.json");

            XlsxTable1Extractor.Extract(fixturePath, outputPath);

            var result =
                JsonSerializer.Deserialize<XlsxTable1Extractor.ExtractionResult>(
                    File.ReadAllText(outputPath));

            return Assert.IsType<XlsxTable1Extractor.ExtractionResult>(result);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void April2013FixturePassesStructuralValidation()
    {
        var result = ExtractFixture();

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.True(
            report.IsValid,
            string.Join(
                Environment.NewLine,
                report.Errors.Select(error =>
                    $"{error.Code}: {error.Message}")));

        Assert.Empty(report.Errors);
        Assert.Empty(report.Warnings);
        Assert.NotEmpty(report.Checks);
    }

    [Fact]
    public void MissingRequiredHeaderCellFailsValidation()
    {
        var result = ExtractFixture();
        result.Cells.RemoveAll(cell => cell.Reference == "C6");

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "required-cell" &&
            check.Message.Contains("C6"));
    }

    [Fact]
    public void InvalidCellReferenceFailsValidation()
    {
        var result = ExtractFixture();
        result.Cells[0].Reference = "A0";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "cell-reference-format");
    }

    [Fact]
    public void DuplicateCellReferenceFailsValidation()
    {
        var result = ExtractFixture();
        result.Cells.Add(new XlsxTable1Extractor.CellData
        {
            Reference = result.Cells[0].Reference,
            ResolvedValue = result.Cells[0].ResolvedValue
        });

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "duplicate-cell-reference");
    }

    [Fact]
    public void MissingObservationFailsValidation()
    {
        var result = ExtractFixture();
        result.Observations.RemoveAll(
            observation => observation.CellReference == "E11");

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "expected-observation" &&
            check.Message.Contains("E11"));
    }

    [Fact]
    public void ChangedIndicatorLabelFailsValidation()
    {
        var result = ExtractFixture();

        var cell = Assert.Single(
            result.Cells,
            item => item.Reference == "A10");

        cell.ResolvedValue = "Unexpected indicator";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "required-cell" &&
            check.Message.Contains("A10"));
    }

    [Fact]
    public void MissingMergedRangeFailsValidation()
    {
        var result = ExtractFixture();
        result.MergedRanges.Remove("A6:B7");

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "merged-range" &&
            check.Message.Contains("A6:B7"));
    }

    [Fact]
    public void MissingHeaderRowFailsValidation()
    {
        var result = ExtractFixture();
        result.HeaderRows.RemoveAll(row => row.RowNumber == 7);

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "header-row" &&
            check.Message.Contains("7"));
    }

    [Fact]
    public void ChangedUnitFailsValidation()
    {
        var result = ExtractFixture();

        var cell = Assert.Single(
            result.Cells,
            item => item.Reference == "B10");

        cell.ResolvedValue = "(1 000 Euro)";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
        Assert.Contains(report.Errors, check =>
            check.Code == "required-cell" &&
            check.Message.Contains("B10"));
    }

    [Fact]
    public void AdditionalObservationProducesWarningRatherThanError()
    {
        var result = ExtractFixture();

        result.Observations.Add(
            new XlsxTable1Extractor.Observation
            {
                SourceRow = 23,
                CellReference = "E23",
                Column = "E",
                ColumnHeader = "Additional column",
                Indicator = "Additional indicator",
                IndicatorSourceRow = 23,
                ResolvedValue = "123"
            });

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.True(report.IsValid);
        Assert.Contains(report.Warnings, check =>
            check.Code == "unexpected-observation");
        Assert.Contains(report.Warnings, check =>
            check.Code == "unexpected-observation-row");
    }

    [Fact]
    public void NullCellEntryFailsValidation()
    {
        var result = ExtractFixture();
        result.Cells.Add(null!);

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void NullObservationEntryFailsValidation()
    {
        var result = ExtractFixture();
        result.Observations.Add(null!);

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void NullHeaderRowEntryFailsValidation()
    {
        var result = ExtractFixture();
        result.HeaderRows.Add(null!);

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void DuplicateHeaderRowFailsValidation()
    {
        var result = ExtractFixture();
        var headerRow = Assert.Single(
            result.HeaderRows,
            row => row.RowNumber == 7);

        result.HeaderRows.Add(headerRow);

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void ChangedHeadingMetadataFailsValidation()
    {
        var result = ExtractFixture();
        result.Headings[0] = "Corrupted heading";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void ChangedFootnoteMetadataFailsValidation()
    {
        var result = ExtractFixture();
        result.Footnotes[0] = "Corrupted footnote";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void ChangedColumnHeaderMetadataFailsValidation()
    {
        var result = ExtractFixture();
        result.ColumnHeaders["C"] = "Corrupted column header";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }

    [Fact]
    public void ChangedObservationHeaderFailsValidation()
    {
        var result = ExtractFixture();
        result.Observations[0].ColumnHeader = "Corrupted observation header";

        var report = XlsxStructureValidator.ValidateTable1(result);

        Assert.False(report.IsValid);
    }
}
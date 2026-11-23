
using System.Globalization;
using System.Text.Json;
using LBICAA.Crawler;
using LBICAA.Verification;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace LBICAA.Tests;

public class XlsxTable1ExtractorTests
{
    private static void CorruptWorkbookCell(
    string workbookPath,
    string cellReference,
    string expectedOriginalValue,
    string replacementValue)
    {
        using var archive = ZipFile.Open(
            workbookPath,
            ZipArchiveMode.Update);

        const string worksheetPath = "xl/worksheets/sheet7.xml";

        var entry = archive.GetEntry(worksheetPath);

        Assert.NotNull(entry);

        XDocument document;

        using (var stream = entry!.Open())
        {
            document = XDocument.Load(stream);
        }

        XNamespace ns =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var cell = Assert.Single(
            document.Descendants(ns + "c"),
            element => (string?)element.Attribute("r") == cellReference);

        var value = cell.Element(ns + "v");

        Assert.NotNull(value);

        Assert.Equal(expectedOriginalValue, value!.Value);

        value.Value = replacementValue;

        entry.Delete();

        var newEntry = archive.CreateEntry(
            worksheetPath,
            CompressionLevel.Optimal);

        using var output = newEntry.Open();

        document.Save(output);
    }
    private static void ReplaceWorkbookCellWithText(
        string workbookPath,
        string cellReference,
        string expectedOriginalValue,
        string replacementValue)
    {
        using var archive = ZipFile.Open(
            workbookPath,
            ZipArchiveMode.Update);

        const string worksheetPath = "xl/worksheets/sheet7.xml";

        var entry = archive.GetEntry(worksheetPath);
        Assert.NotNull(entry);

        XDocument document;

        using (var stream = entry!.Open())
        {
            document = XDocument.Load(stream);
        }

        XNamespace ns =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var cell = Assert.Single(
            document.Descendants(ns + "c"),
            element => (string?)element.Attribute("r") == cellReference);

        var value = cell.Element(ns + "v");
        Assert.NotNull(value);
        Assert.Equal(expectedOriginalValue, value!.Value);

        cell.SetAttributeValue("t", "inlineStr");
        value.Remove();

        cell.Add(
            new XElement(
                ns + "is",
                new XElement(ns + "t", replacementValue)));

        entry.Delete();

        var newEntry = archive.CreateEntry(
            worksheetPath,
            CompressionLevel.Optimal);

        using var output = newEntry.Open();
        document.Save(output);
    }
    private static void RemoveWorkbookCell(
        string workbookPath,
        string cellReference)
    {
        using var archive = ZipFile.Open(
            workbookPath,
            ZipArchiveMode.Update);

        const string worksheetPath = "xl/worksheets/sheet7.xml";

        var entry = archive.GetEntry(worksheetPath);
        Assert.NotNull(entry);

        XDocument document;

        using (var stream = entry!.Open())
        {
            document = XDocument.Load(stream);
        }

        XNamespace ns =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var cell = Assert.Single(
            document.Descendants(ns + "c"),
            element => (string?)element.Attribute("r") == cellReference);

        cell.Remove();

        entry.Delete();

        var newEntry = archive.CreateEntry(
            worksheetPath,
            CompressionLevel.Optimal);

        using var output = newEntry.Open();
        document.Save(output);
    }
    private static void DuplicateWorkbookCell(
        string workbookPath,
        string cellReference)
    {
        using var archive = ZipFile.Open(
            workbookPath,
            ZipArchiveMode.Update);

        const string worksheetPath = "xl/worksheets/sheet7.xml";

        var entry = archive.GetEntry(worksheetPath);
        Assert.NotNull(entry);

        XDocument document;

        using (var stream = entry!.Open())
        {
            document = XDocument.Load(stream);
        }

        XNamespace ns =
            "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var cell = Assert.Single(
            document.Descendants(ns + "c"),
            element => (string?)element.Attribute("r") == cellReference);

        var rowNumber = new string(
            cellReference.Where(char.IsDigit).ToArray());

        var row = Assert.Single(
            document.Descendants(ns + "row"),
            element => (string?)element.Attribute("r") == rowNumber);

        // Add a second copy of the exact same source cell.
        row.Add(new XElement(cell));

        entry.Delete();

        var newEntry = archive.CreateEntry(
            worksheetPath,
            CompressionLevel.Optimal);

        using var output = newEntry.Open();
        document.Save(output);
    }
    private static XlsxTable1Extractor.ExtractionResult ExtractFixture()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(
            File.Exists(fixturePath),
            $"Test fixture not found: {fixturePath}");

        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "LBICAA.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var outputPath = Path.Combine(
                temporaryDirectory,
                "table1.json");

            XlsxTable1Extractor.Extract(fixturePath, outputPath);

            var json = File.ReadAllText(outputPath);

            var result =
                JsonSerializer.Deserialize<XlsxTable1Extractor.ExtractionResult>(
                    json);

            return Assert.IsType<XlsxTable1Extractor.ExtractionResult>(result);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
                Directory.Delete(temporaryDirectory, recursive: true);
        }
    }

    private static decimal Number(
        XlsxTable1Extractor.ExtractionResult result,
        int row)
    {
        var observation = Assert.Single(
            result.Observations,
            item => item.SourceRow == row && item.Column == "E");

        Assert.NotNull(observation.ResolvedValue);

        return decimal.Parse(
            observation.ResolvedValue,
            CultureInfo.InvariantCulture);
    }

    private static void AssertCategoriesReconcile(
        XlsxTable1Extractor.ExtractionResult result,
        int parentRow,
        int[] categoryRows)
    {
        var parent = Number(result, parentRow);

        var categoryTotal = categoryRows
            .Sum(row => Number(result, row));

        Assert.Equal(parent, categoryTotal);
    }

    [Fact]
    public void ExtractsExpectedObservationCount()
    {
        var result = ExtractFixture();

        Assert.Equal(112, result.Observations.Count);
        Assert.Equal(14, result.Observations
            .Select(item => item.SourceRow)
            .Distinct()
            .Count());
    }

    [Fact]
    public void PreservesIndicatorAndCategoryHierarchy()
    {
        var result = ExtractFixture();

        var housingHours = Assert.Single(
            result.Observations,
            item => item.CellReference == "E11");

        Assert.Equal(
            "Geleistete Arbeitsstunden",
            housingHours.Indicator);

        Assert.Equal("Wohnungsbau", housingHours.Category);
        Assert.Equal(10, housingHours.IndicatorSourceRow);

        var parentHours = Assert.Single(
            result.Observations,
            item => item.CellReference == "E10");

        Assert.Equal(
            "Geleistete Arbeitsstunden",
            parentHours.Indicator);

        Assert.Null(parentHours.Category);
        Assert.Equal(10, parentHours.IndicatorSourceRow);
    }

    [Fact]
    public void PreservesUnitsAndOriginalCellReferences()
    {
        var result = ExtractFixture();

        var hours = Assert.Single(
            result.Observations,
            item => item.CellReference == "E10");

        Assert.Equal("(1 000)", hours.Unit);
        Assert.Equal("846", hours.ResolvedValue);
        Assert.Null(hours.DataType);

        var turnover = Assert.Single(
            result.Observations,
            item => item.CellReference == "E14");

        Assert.Equal("(1 000 Euro)", turnover.Unit);
        Assert.Equal("157325", turnover.ResolvedValue);

        var orderIntake = Assert.Single(
            result.Observations,
            item => item.CellReference == "E19");

        Assert.Null(orderIntake.Unit);
        Assert.Equal("94271", orderIntake.ResolvedValue);

                // Indicator row: label and unit originate from the same row.
        Assert.Equal("A10", hours.IndicatorCellReference);
        Assert.Equal("A10", hours.SourceLabelCellReference);
        Assert.Equal("B10", hours.UnitCellReference);

        // Category row: inherits indicator and unit from its parent.
        var housing = Assert.Single(
            result.Observations,
            item => item.CellReference == "E11");

        Assert.Equal("Geleistete Arbeitsstunden", housing.Indicator);
        Assert.Equal("Wohnungsbau", housing.Category);
        Assert.Equal("A10", housing.IndicatorCellReference);
        Assert.Equal("A11", housing.SourceLabelCellReference);
        Assert.Equal("B10", housing.UnitCellReference);

        // An indicator without a unit must not acquire a fictitious reference.
        Assert.Null(orderIntake.UnitCellReference);
    }

    [Fact]
    public void PreservesFormulaAndCachedValue()
    {
        var result = ExtractFixture();

        var cell = Assert.Single(
            result.Cells,
            item => item.Reference == "F9");

        Assert.Equal("SUM(E9*100/C9-100)", cell.Formula);
        Assert.NotNull(cell.RawValue);
        Assert.NotNull(cell.ResolvedValue);
    }

    [Fact]
    public void PreservesSourceAndWorksheetProvenance()
    {
        var result = ExtractFixture();

        Assert.Equal(
            "hamburg-construction-april-2013.xlsx",
            result.SourceFile);

        Assert.Equal("tab.1 (S.7)", result.WorksheetName);
        Assert.Equal(
            "xl/worksheets/sheet7.xml",
            result.WorksheetPath);

        Assert.Contains("A6:B7", result.MergedRanges);
        Assert.NotEmpty(result.HeaderRows);
        Assert.NotEmpty(result.Footnotes);
    }

    [Fact]
    public void April2013ParentValuesReconcileWithCategories()
    {
        var result = ExtractFixture();

        // Working hours
        AssertCategoriesReconcile(result, 10, [11, 12, 13]);

        // Construction turnover
        AssertCategoriesReconcile(result, 14, [15, 16, 17]);

        // Order intake
        AssertCategoriesReconcile(result, 19, [20, 21, 22]);
    }

    [Fact]
    public void ExtractedApril2013ObservationsPassReconciliationVerifier()
    {
        var extraction = ExtractFixture();

        var observations = extraction.Observations
            .Select(item => new ReconciliationObservation(
                item.CellReference,
                item.Column,
                item.ResolvedValue,
                item.DataType))
            .ToList();

        var rules = new[]
        {
            new ReconciliationRule(
                "Working hours",
                "E",
                "E10",
                ["E11", "E12", "E13"]),

            new ReconciliationRule(
                "Turnover",
                "E",
                "E14",
                ["E15", "E16", "E17"]),

            new ReconciliationRule(
                "Order intake",
                "E",
                "E19",
                ["E20", "E21", "E22"])
        };

        foreach (var rule in rules)
        {
            var result = ReconciliationVerifier.Verify(
                observations,
                rule);

            Assert.Equal(
                ReconciliationStatus.Passed,
                result.Status);
        }
    }
    [Fact]
    public void ReconciliationVerifierDetectsCorruptedExtractedValue()
    {
        var extraction = ExtractFixture();

        var observations = extraction.Observations
            .Select(item => new ReconciliationObservation(
                item.CellReference,
                item.Column,
                item.ResolvedValue,
                item.DataType))
            .ToList();

        // Deliberately corrupt E11: 274 becomes 275.
        var index = observations.FindIndex(
            item => item.CellReference == "E11" &&
                    item.Column == "E");

        Assert.True(index >= 0);

        observations[index] = observations[index] with
        {
            ResolvedValue = "275"
        };

        var rule = new ReconciliationRule(
            "Working hours",
            "E",
            "E10",
            ["E11", "E12", "E13"]);

        var result = ReconciliationVerifier.Verify(
            observations,
            rule);

        Assert.Equal(ReconciliationStatus.Failed, result.Status);
        Assert.Equal(-1m, result.Difference);
    }
    [Fact]
    public void ExtractorAndVerifierDetectCorruptedWorkbookCell()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(File.Exists(fixturePath));

        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "LBICAA.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var corruptedWorkbookPath = Path.Combine(
                temporaryDirectory,
                "corrupted-workbook.xlsx");

            var outputPath = Path.Combine(
                temporaryDirectory,
                "table1.json");

            File.Copy(
                fixturePath,
                corruptedWorkbookPath);

            // Change the original workbook value from 274 to 275.
            CorruptWorkbookCell(
                corruptedWorkbookPath,
                "E11",
                "274",
                "275");

            

            // Run the real extractor against the corrupted workbook.
            XlsxTable1Extractor.Extract(
                corruptedWorkbookPath,
                outputPath);

            var json = File.ReadAllText(outputPath);

            var extraction =
                JsonSerializer.Deserialize<XlsxTable1Extractor.ExtractionResult>(
                    json);

            Assert.NotNull(extraction);

            // Confirm the corruption survived extraction.
            Assert.Equal(275m, Number(extraction!, 11));

            var observations = extraction.Observations
                .Select(item => new ReconciliationObservation(
                    item.CellReference,
                    item.Column,
                    item.ResolvedValue,
                    item.DataType))
                .ToList();

            var rule = new ReconciliationRule(
                "Working hours",
                "E",
                "E10",
                ["E11", "E12", "E13"]);

            var result = ReconciliationVerifier.Verify(
                observations,
                rule);

            Assert.Equal(
                ReconciliationStatus.Failed,
                result.Status);

            Assert.Equal(846m, result.ParentValue);
            Assert.Equal(847m, result.CategoryTotal);
            Assert.Equal(-1m, result.Difference);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(
                    temporaryDirectory,
                    recursive: true);
            }
        }
    }
    [Fact]
public void ExtractorAndVerifierTreatTextMarkerAsNotVerifiable()
{
    var fixturePath = Path.Combine(
        AppContext.BaseDirectory,
        "Fixtures",
        "hamburg-construction-april-2013.xlsx");

    var temporaryDirectory = Path.Combine(
        Path.GetTempPath(),
        "LBICAA.Tests",
        Guid.NewGuid().ToString("N"));

    Directory.CreateDirectory(temporaryDirectory);

    try
    {
        var corruptedWorkbookPath = Path.Combine(
            temporaryDirectory,
            "text-marker-workbook.xlsx");

        var outputPath = Path.Combine(
            temporaryDirectory,
            "table1.json");

        File.Copy(fixturePath, corruptedWorkbookPath);
        ReplaceWorkbookCellWithText(
            corruptedWorkbookPath,
            "E11",
            "274",
            "·");
       

        XlsxTable1Extractor.Extract(
            corruptedWorkbookPath,
            outputPath);

        var json = File.ReadAllText(outputPath);

        var extraction =
            JsonSerializer.Deserialize<XlsxTable1Extractor.ExtractionResult>(
                json);

        Assert.NotNull(extraction);

        var extractedCell = Assert.Single(
            extraction!.Cells,
            item => item.Reference == "E11");

        Assert.Equal("inlineStr", extractedCell.DataType);
        Assert.Equal("·", extractedCell.ResolvedValue);

        var observations = extraction.Observations
            .Select(item => new ReconciliationObservation(
                item.CellReference,
                item.Column,
                item.ResolvedValue,
                item.DataType))
            .ToList();

        var result = ReconciliationVerifier.Verify(
            observations,
            new ReconciliationRule(
                "Working hours",
                "E",
                "E10",
                ["E11", "E12", "E13"]));

        Assert.Equal(
            ReconciliationStatus.NotVerifiable,
            result.Status);

        Assert.Equal(846m, result.ParentValue);
        Assert.Null(result.CategoryTotal);
        Assert.Null(result.Difference);
        Assert.Contains("non-numeric", result.Message);
    }
    finally
    {
        if (Directory.Exists(temporaryDirectory))
        {
            Directory.Delete(
                temporaryDirectory,
                recursive: true);
        }
    }
}

    [Fact]
    public void ExtractorAndVerifierTreatMissingSourceCellAsNotVerifiable()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(File.Exists(fixturePath));

        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "LBICAA.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var workbookPath = Path.Combine(
                temporaryDirectory,
                "missing-cell-workbook.xlsx");

            var outputPath = Path.Combine(
                temporaryDirectory,
                "table1.json");

            File.Copy(fixturePath, workbookPath);

            // Deliberately remove E11 from the original worksheet.
            RemoveWorkbookCell(workbookPath, "E11");

            XlsxTable1Extractor.Extract(
                workbookPath,
                outputPath);

            var json = File.ReadAllText(outputPath);

            var extraction =
                JsonSerializer.Deserialize<XlsxTable1Extractor.ExtractionResult>(
                    json);

            Assert.NotNull(extraction);

            // The extractor must not invent the missing observation.
            Assert.DoesNotContain(
                extraction!.Observations,
                item => item.CellReference == "E11");

            var observations = extraction.Observations
                .Select(item => new ReconciliationObservation(
                    item.CellReference,
                    item.Column,
                    item.ResolvedValue,
                    item.DataType))
                .ToList();

            var result = ReconciliationVerifier.Verify(
                observations,
                new ReconciliationRule(
                    "Working hours",
                    "E",
                    "E10",
                    ["E11", "E12", "E13"]));

            Assert.Equal(
                ReconciliationStatus.NotVerifiable,
                result.Status);

            Assert.Equal(846m, result.ParentValue);
            Assert.Null(result.CategoryTotal);
            Assert.Null(result.Difference);
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(
                    temporaryDirectory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void ExtractorRejectsDuplicateSourceCell()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(File.Exists(fixturePath));

        var temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "LBICAA.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryDirectory);

        try
        {
            var workbookPath = Path.Combine(
                temporaryDirectory,
                "duplicate-cell-workbook.xlsx");

            var outputPath = Path.Combine(
                temporaryDirectory,
                "table1.json");

            File.Copy(fixturePath, workbookPath);

            // Deliberately insert a second E11 into the worksheet.
            DuplicateWorkbookCell(workbookPath, "E11");

            // The extractor must reject the ambiguous source structure.
            var exception = Record.Exception(
                () => XlsxTable1Extractor.Extract(
                    workbookPath,
                    outputPath));

            Assert.NotNull(exception);

            Assert.Contains(
                "E11",
                exception!.ToString());
        }
        finally
        {
            if (Directory.Exists(temporaryDirectory))
            {
                Directory.Delete(
                    temporaryDirectory,
                    recursive: true);
            }
        }
    }

    [Fact]
    public void MissingWorksheetPart_FailsExtraction()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var workbookPath = Path.Combine(tempDirectory, "missing-worksheet.xlsx");
        var outputPath = Path.Combine(tempDirectory, "output.json");

        try
        {
            using (var source = ZipFile.OpenRead(fixturePath))
            using (var destination = ZipFile.Open(workbookPath, ZipArchiveMode.Create))
            {
                foreach (var entry in source.Entries)
                {
                    if (entry.FullName == "xl/worksheets/sheet7.xml")
                        continue;

                    var copiedEntry = destination.CreateEntry(entry.FullName);

                    using var input = entry.Open();
                    using var output = copiedEntry.Open();
                    input.CopyTo(output);
                }
            }

            var exception = Assert.Throws<InvalidDataException>(
                () => XlsxTable1Extractor.Extract(workbookPath, outputPath));

            Assert.Contains("xl/worksheets/sheet7.xml", exception.Message);
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void MalformedWorksheetXml_FailsExtraction()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "hamburg-construction-april-2013.xlsx");

        Assert.True(File.Exists(fixturePath), $"Fixture not found: {fixturePath}");

        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var workbookPath = Path.Combine(tempDirectory, "malformed-worksheet.xlsx");
        var outputPath = Path.Combine(tempDirectory, "output.json");

        try
        {
            using (var source = ZipFile.OpenRead(fixturePath))
            using (var destination = ZipFile.Open(workbookPath, ZipArchiveMode.Create))
            {
                foreach (var entry in source.Entries)
                {
                    var copiedEntry = destination.CreateEntry(entry.FullName);

                    using var output = copiedEntry.Open();

                    if (entry.FullName == "xl/worksheets/sheet7.xml")
                    {
                        using var writer = new StreamWriter(output);
                        writer.Write("<worksheet");
                    }
                    else
                    {
                        using var input = entry.Open();
                        input.CopyTo(output);
                    }
                }
            }

            Assert.Throws<XmlException>(
                () => XlsxTable1Extractor.Extract(workbookPath, outputPath));
        }
        finally
        {
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, true);
        }
    }

    [Fact]
    public void RecordsExtractionProvenance()
    {
        var result = ExtractFixture();

        var provenance = result.Provenance;

        Assert.Equal("1.0", provenance.SchemaVersion);
        Assert.False(string.IsNullOrWhiteSpace(provenance.ExtractorVersion));
        Assert.Equal(64, provenance.SourceSha256.Length);
        Assert.All(
            provenance.SourceSha256,
            character => Assert.True(Uri.IsHexDigit(character)));

        Assert.Equal(TimeSpan.Zero, provenance.ExtractedAtUtc.Offset);
        Assert.EndsWith(
    Path.Combine("Fixtures", "hamburg-construction-april-2013.xlsx"),
    provenance.SourcePath,
    StringComparison.OrdinalIgnoreCase);
        Assert.False(string.IsNullOrWhiteSpace(provenance.GitCommit));
        Assert.Equal(40, provenance.GitCommit.Length);
        Assert.All(
            provenance.GitCommit,
            character => Assert.True(Uri.IsHexDigit(character)));

        Assert.True(provenance.GitWorkingTreeDirty);
    }

    [Fact]
    public void EveryObservationHasCompleteAndConsistentProvenance()
    {
        var result = ExtractFixture();

        Assert.Equal(112, result.Observations.Count);

        var expectedHeaders = new Dictionary<string, string[]>
        {
            ["C"] = ["C6"],
            ["D"] = ["D6"],
            ["E"] = ["E6"],
            ["F"] = ["F6", "F7"],
            ["G"] = ["F6", "G7"],
            ["H"] = ["H6", "H7"],
            ["I"] = ["H6", "I7"],
            ["J"] = ["J7"]
        };

        Assert.All(result.Observations, observation =>
        {
            Assert.False(
                string.IsNullOrWhiteSpace(
                    observation.SourceLabelCellReference));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    observation.IndicatorCellReference));

            Assert.NotEmpty(
                observation.ColumnHeaderCellReferences);

            Assert.True(
                expectedHeaders.ContainsKey(observation.Column));

            Assert.Equal(
                expectedHeaders[observation.Column],
                observation.ColumnHeaderCellReferences);

            Assert.Matches(
                @"^A\d+$",
                observation.SourceLabelCellReference!);

            Assert.Matches(
                @"^A\d+$",
                observation.IndicatorCellReference!);

            if (observation.UnitCellReference is not null)
            {
                Assert.Matches(
                    @"^B\d+$",
                    observation.UnitCellReference);
            }
        });
    }

    [Fact]
    public void PreservesColumnHeaderSourceReferences()
    {
        var result = ExtractFixture();

        var expected = new Dictionary<string, (string Header, string[] References)>
        {
            ["F"] = ("Veränderung in % April 2013 gegenüber — April 2012", ["F6", "F7"]),
            ["G"] = ("Veränderung in % April 2013 gegenüber — März 2013", ["F6", "G7"]),
            ["H"] = ("Januar bis April2 — 2012", ["H6", "H7"]),
            ["I"] = ("Januar bis April2 — 2013", ["H6", "I7"]),
            ["J"] = ("Verän-derung in %", ["J7"])
        };

        foreach (var (column, expectation) in expected)
        {
            var observation = Assert.Single(
                result.Observations,
                item => item.CellReference == $"{column}9");

            Assert.Equal(expectation.Header, observation.ColumnHeader);
            Assert.Equal(
                expectation.References,
                observation.ColumnHeaderCellReferences);
        }
    }
}


using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Diagnostics;

namespace LBICAA.Crawler;

public static class XlsxTable1Extractor
{
    private static readonly XNamespace Main =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static readonly XNamespace Relationships =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

    private static readonly XNamespace PackageRelationships =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    private static (string? Commit, bool? Dirty) GetGitMetadata()
    {
        try
        {
            string RunGit(string arguments)
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        WorkingDirectory = AppContext.BaseDirectory,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                    throw new InvalidOperationException("Git command failed.");

                return output.Trim();
            }

            var commit = RunGit("rev-parse --verify HEAD");
            var status = RunGit("status --porcelain");

            return (commit, !string.IsNullOrWhiteSpace(status));
        }
        catch
        {
            return (null, null);
        }
    }
    public static void Extract(string xlsxPath, string outputPath)
    {
        using var archive = ZipFile.OpenRead(xlsxPath);

        var workbook = LoadXml(archive, "xl/workbook.xml");
        var workbookRelationships = LoadXml(archive, "xl/_rels/workbook.xml.rels");

        var relationshipTargets = workbookRelationships
            .Root!
            .Elements(PackageRelationships + "Relationship")
            .ToDictionary(
                element => (string)element.Attribute("Id")!,
                element => (string)element.Attribute("Target")!);

        var sheet = workbook
            .Descendants(Main + "sheet")
            .Single(element =>
                (string?)element.Attribute("name") == "tab.1 (S.7)");

        var relationshipId = (string)sheet.Attribute(Relationships + "id")!;
        var target = relationshipTargets[relationshipId];

        var worksheetPath = target.StartsWith("/", StringComparison.Ordinal)
            ? target.TrimStart('/')
            : "xl/" + target.TrimStart('/');

        worksheetPath = worksheetPath.Replace('\\', '/');

        var worksheet = LoadXml(archive, worksheetPath);
        var mergedRanges = worksheet
            .Descendants(Main + "mergeCell")
            .Select(element => (string)element.Attribute("ref")!)
            .ToList();
        var sharedStrings = ReadSharedStrings(archive);
        var numberFormats = ReadNumberFormats(archive);
        var styleNumberFormats = ReadStyleNumberFormats(archive);

        var cells = worksheet
            .Descendants(Main + "c")
            .Select(element => ReadCell(
                element,
                sharedStrings,
                numberFormats,
                styleNumberFormats))
            .ToList();

        var cellsByReference = cells.ToDictionary(cell => cell.Reference);

        var columnHeaders = new Dictionary<string, string>
        {
            ["A"] = GetCellText(cellsByReference, "A6") ?? "",
            ["B"] = GetCellText(cellsByReference, "B6") ?? "",
            ["C"] = GetCellText(cellsByReference, "C6") ?? "",
            ["D"] = GetCellText(cellsByReference, "D6") ?? "",
            ["E"] = GetCellText(cellsByReference, "E6") ?? "",
            ["F"] = JoinHeader(
                GetCellText(cellsByReference, "F6"),
                GetCellText(cellsByReference, "F7")),
            ["G"] = JoinHeader(
                GetCellText(cellsByReference, "F6"),
                GetCellText(cellsByReference, "G7")),
            ["H"] = JoinHeader(
                GetCellText(cellsByReference, "H6"),
                GetCellText(cellsByReference, "H7")),
            ["I"] = JoinHeader(
                GetCellText(cellsByReference, "H6"),
                GetCellText(cellsByReference, "I7")),
            ["J"] = GetCellText(cellsByReference, "J7") ?? ""
        };

        var headerRows = new List<HeaderRow>
        {
            new()
            {
                RowNumber = 6,
                Cells = cells
                    .Where(cell => Regex.IsMatch(cell.Reference, @"^[A-J]6$"))
                    .ToList()
            },
            new()
            {
                RowNumber = 7,
                Cells = cells
                    .Where(cell => Regex.IsMatch(cell.Reference, @"^[A-J]7$"))
                    .ToList()
            }
        };

        var observations = new List<Observation>();

        // Each row points to the row containing its parent indicator.
        // Parent rows point to themselves.
        var parentRows = new Dictionary<int, int>
        {
            [9] = 9,
            [10] = 10,
            [11] = 10,
            [12] = 10,
            [13] = 10,
            [14] = 14,
            [15] = 14,
            [16] = 14,
            [17] = 14,
            [18] = 18,
            [19] = 19,
            [20] = 19,
            [21] = 19,
            [22] = 19
        };

        var columnHeaderCellReferences = new Dictionary<string, List<string>>
        {
            ["A"] = ["A6"],
            ["B"] = ["B6"],
            ["C"] = ["C6"],
            ["D"] = ["D6"],
            ["E"] = ["E6"],
            ["F"] = ["F6", "F7"],
            ["G"] = ["F6", "G7"],
            ["H"] = ["H6", "H7"],
            ["I"] = ["H6", "I7"],
            ["J"] = ["J7"]
        };

        foreach (var rowNumber in Enumerable.Range(9, 14))
        {
            if (!parentRows.TryGetValue(rowNumber, out var parentRow))
                continue;

            var sourceLabel = GetCellText(
                cellsByReference,
                $"A{rowNumber}");

            var indicator = GetCellText(
                cellsByReference,
                $"A{parentRow}")?.Trim();

            var category = rowNumber == parentRow
                ? null
                : sourceLabel?.Trim();

            var unit = GetCellText(
                cellsByReference,
                $"B{parentRow}")?.Trim();

            foreach (var column in new[] { "C", "D", "E", "F", "G", "H", "I", "J" })
            {
                var reference = $"{column}{rowNumber}";

                if (!cellsByReference.TryGetValue(reference, out var cell))
                    continue;

                observations.Add(new Observation
                {
                    DataType = cell.DataType,
                    SourceRow = rowNumber,
                    CellReference = reference,
                    Indicator = indicator,
                    Category = category,
                    SourceLabel = sourceLabel,
                    IndicatorSourceRow = parentRow,
                    Unit = unit,
                    Column = column,
                    ColumnHeader = columnHeaders[column],
                    RawValue = cell.RawValue,
                    ResolvedValue = cell.ResolvedValue,
                    Formula = cell.Formula,
                    StyleIndex = cell.StyleIndex,
                    NumberFormatId = cell.NumberFormatId,
                    NumberFormatCode = cell.NumberFormatCode,
                    SourceLabelCellReference = $"A{rowNumber}",
                    IndicatorCellReference = $"A{parentRow}",
                    UnitCellReference = unit is null ? null : $"B{parentRow}",
                    ColumnHeaderCellReferences = columnHeaderCellReferences[column],
                });
            }
        }
                var sourceFullPath = Path.GetFullPath(xlsxPath);

        var sourceSha256 = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                File.ReadAllBytes(sourceFullPath)));

        var extractorVersion =
            typeof(XlsxTable1Extractor).Assembly
                .GetName()
                .Version?
                .ToString() ?? "unknown";
        
        var gitMetadata = GetGitMetadata();

        var result = new ExtractionResult
        {
            SourceFile = Path.GetFileName(xlsxPath),
            Provenance = new ExtractionProvenance
            {
                SchemaVersion = "1.0",
                ExtractorVersion = extractorVersion,
                GitCommit = gitMetadata.Commit,
                GitWorkingTreeDirty = gitMetadata.Dirty,
                SourceSha256 = sourceSha256,
                ExtractedAtUtc = DateTimeOffset.UtcNow,
                SourcePath = sourceFullPath
            },
            WorksheetName = (string?)sheet.Attribute("name"),
            WorksheetPath = worksheetPath,
            MergedRanges = mergedRanges,
            HeaderRows = headerRows,
            ColumnHeaders = columnHeaders,
            Headings = Enumerable.Range(1, 4)
                .Select(row => GetCellText(cellsByReference, $"A{row}"))
                .OfType<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList(),
                        Footnotes = Enumerable.Range(24, 3)
                .Select(row => GetCellText(cellsByReference, $"A{row}"))
                .OfType<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList(),
            Cells = cells,
            Observations = observations
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        
        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(
            outputPath,
            JsonSerializer.Serialize(result, options));
    }

    private static XDocument LoadXml(ZipArchive archive, string path)
    {
        var entry = archive.GetEntry(path)
            ?? throw new InvalidDataException($"Missing XLSX entry: {path}");

        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static List<string> ReadSharedStrings(ZipArchive archive)
    {
        var entry = archive.GetEntry("xl/sharedStrings.xml");

        if (entry is null)
            return [];

        using var stream = entry.Open();
        var document = XDocument.Load(stream);

        return document
            .Descendants(Main + "si")
            .Select(item => string.Concat(
                item.Descendants(Main + "t").Select(text => text.Value)))
            .ToList();
    }

    private static Dictionary<int, string> ReadNumberFormats(
        ZipArchive archive)
    {
        var document = LoadXml(archive, "xl/styles.xml");

        return document
            .Descendants(Main + "numFmt")
            .Where(element => element.Attribute("numFmtId") is not null)
            .ToDictionary(
                element => (int)element.Attribute("numFmtId")!,
                element => (string)element.Attribute("formatCode")!);
    }

    private static CellData ReadCell(
        XElement element,
        IReadOnlyList<string> sharedStrings,
        IReadOnlyDictionary<int, string> numberFormats,
        IReadOnlyDictionary<int, int> styleNumberFormats)
    {
        var reference = (string)element.Attribute("r")!;
        var type = (string?)element.Attribute("t");
        var styleText = (string?)element.Attribute("s");

        int? styleIndex = styleText is null
            ? null
            : int.Parse(styleText, CultureInfo.InvariantCulture);

        var rawValue = element.Element(Main + "v")?.Value;
        var formula = element.Element(Main + "f")?.Value;

        string? resolvedValue = type switch
        {
            "s" when rawValue is not null =>
                sharedStrings[int.Parse(rawValue, CultureInfo.InvariantCulture)],

            "inlineStr" =>
                string.Concat(element.Descendants(Main + "t")
                    .Select(text => text.Value)),

            "str" => rawValue,

            "b" => rawValue == "1" ? "TRUE" : "FALSE",

            _ => rawValue
        };

        int? numberFormatId = null;
        string? numberFormatCode = null;

        if (styleIndex.HasValue &&
            styleNumberFormats.TryGetValue(styleIndex.Value, out var formatId))
        {
            numberFormatId = formatId;
        }

        if (numberFormatId.HasValue)
            numberFormats.TryGetValue(numberFormatId.Value, out numberFormatCode);

        return new CellData
        {
            Reference = reference,
            DataType = type,
            RawValue = rawValue,
            ResolvedValue = resolvedValue,
            Formula = formula,
            StyleIndex = styleIndex,
            NumberFormatId = numberFormatId,
            NumberFormatCode = numberFormatCode
        };
    }

        private static Dictionary<int, int> ReadStyleNumberFormats(
        ZipArchive archive)
    {
        var styles = LoadXml(archive, "xl/styles.xml");
        var cellFormats = styles.Root?.Element(Main + "cellXfs");

        if (cellFormats is null)
            return [];

        return cellFormats
            .Elements(Main + "xf")
            .Select((element, index) => new
            {
                Index = index,
                FormatId = (int?)element.Attribute("numFmtId") ?? 0
            })
            .ToDictionary(item => item.Index, item => item.FormatId);
    }
    private static string JoinHeader(params string?[] parts)
    {
        return string.Join(
            " — ",
            parts
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => Regex.Replace(value!.Trim(), @"\s+", " ")));
    }
    private static string? GetCellText(
        IReadOnlyDictionary<string, CellData> cells,
        string reference)
    {
        return cells.TryGetValue(reference, out var cell)
            ? cell.ResolvedValue
            : null;
    }
    public sealed class ExtractionProvenance
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string ExtractorVersion { get; set; } = "1.0.0";
        public string SourceSha256 { get; set; } = string.Empty;
        public DateTimeOffset ExtractedAtUtc { get; set; }
        public string SourcePath { get; set; } = string.Empty;
        public string? GitCommit { get; set; }
        public bool? GitWorkingTreeDirty { get; set; }
    }
    public sealed class ExtractionResult
    {
        public string? SourceFile { get; set; }
        public string? WorksheetName { get; set; }
        public string? WorksheetPath { get; set; }
        public List<string> Headings { get; set; } = [];
        public List<string> MergedRanges { get; set; } = [];
        public List<HeaderRow> HeaderRows { get; set; } = [];
        public Dictionary<string, string> ColumnHeaders { get; set; } = [];
        public List<string> Footnotes { get; set; } = [];
        public List<CellData> Cells { get; set; } = [];
        public List<Observation> Observations { get; set; } = [];
        public ExtractionProvenance Provenance { get; set; } = new();
    }
    public sealed class HeaderRow
    {
        public int RowNumber { get; set; }
        public List<CellData> Cells { get; set; } = [];
    }
    public sealed class CellData
    {
        public string Reference { get; set; } = "";
        public string? DataType { get; set; }
        public string? RawValue { get; set; }
        public string? ResolvedValue { get; set; }
        public string? Formula { get; set; }
        public int? StyleIndex { get; set; }
        public int? NumberFormatId { get; set; }
        public string? NumberFormatCode { get; set; }
    }

    public sealed class Observation
    {
        public int SourceRow { get; set; }
        public string CellReference { get; set; } = "";
        public string? Unit { get; set; }
        public string Column { get; set; } = "";
        public string ColumnHeader { get; set; } = "";
        public string? RawValue { get; set; }
        public string? ResolvedValue { get; set; }
        public string? Formula { get; set; }
        public int? StyleIndex { get; set; }
        public int? NumberFormatId { get; set; }
        public string? NumberFormatCode { get; set; }
        public string? Indicator { get; set; }
        public string? Category { get; set; }
        public string? SourceLabel { get; set; }
        public string? DataType { get; set; }
        public int IndicatorSourceRow { get; set; }

        public string? SourceLabelCellReference { get; set; }
        public string? IndicatorCellReference { get; set; }
        public string? UnitCellReference { get; set; }
        public List<string> ColumnHeaderCellReferences { get; set; } = [];
    }
}
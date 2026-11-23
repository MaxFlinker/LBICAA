using System.Text.RegularExpressions;

namespace LBICAA.Crawler;

public enum XlsxStructureIssueSeverity
{
    Error,
    Warning
}

public sealed record XlsxStructureCheck(
    string Code,
    bool Passed,
    XlsxStructureIssueSeverity Severity,
    string Message);

public sealed class XlsxStructureValidationReport
{
    public IReadOnlyList<XlsxStructureCheck> Checks { get; }

    public IReadOnlyList<XlsxStructureCheck> Errors =>
        Checks.Where(check =>
            !check.Passed &&
            check.Severity == XlsxStructureIssueSeverity.Error).ToArray();

    public IReadOnlyList<XlsxStructureCheck> Warnings =>
        Checks.Where(check =>
            !check.Passed &&
            check.Severity == XlsxStructureIssueSeverity.Warning).ToArray();

    public bool IsValid => Errors.Count == 0;

    internal XlsxStructureValidationReport(
        IEnumerable<XlsxStructureCheck> checks)
    {
        Checks = checks.ToArray();
    }
}

public static class XlsxStructureValidator
{
    private static readonly string[] ExpectedColumns =
        ["C", "D", "E", "F", "G", "H", "I", "J"];

    private static readonly int[] ExpectedRows =
        Enumerable.Range(9, 14).ToArray();

    private static readonly Dictionary<int, int> ParentRows = new()
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

    private static readonly Dictionary<string, string> ExpectedCells =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["A1"] = "1. Vorbereitende Baustellenarbeiten, Hoch- und Tiefbau (Bauhauptgewerbe) in Hamburg",
            ["A2"] = "1.1 Tätige Personen, geleistete Arbeitsstunden, Umsatz, Bruttoentgelte",
            ["A3"] = "alle Betriebe (hochgerechnet1)",
            ["A4"] = "und Auftragseingang",

            ["A6"] = "Merkmal",
            ["C6"] = "April 2012",
            ["D6"] = "März 2013",
            ["E6"] = "April 2013",
            ["F6"] = "Veränderung in % April 2013 gegenüber",
            ["F7"] = "April 2012",
            ["G7"] = "März 2013",
            ["H6"] = "Januar bis April2",
            ["H7"] = "2012",
            ["I7"] = "2013",
            ["J7"] = "Verän-derung in %",

            ["A9"] = "Tätige Personen",
            ["A10"] = "Geleistete Arbeitsstunden",
            ["A11"] = "Wohnungsbau",
            ["A12"] = "gewerbl. und industrieller Bau3",
            ["A13"] = "öffentlicher und Verkehrsbau",
            ["A14"] = "Baugewerblicher Umsatz",
            ["A15"] = "Wohnungsbau",
            ["A16"] = "gewerbl. und industrieller Bau3",
            ["A17"] = "öffentlicher und Verkehrsbau",
            ["A18"] = "Bruttoentgelte",
            ["A19"] = "Auftragseingang",
            ["A20"] = "Wohnungsbau",
            ["A21"] = "gewerbl. und industrieller Bau3",
            ["A22"] = "öffentlicher und Verkehrsbau",

            ["B10"] = "(1 000)",
            ["B14"] = "(1 000 Euro)",
            ["B18"] = "(1 000 Euro)",

            ["A24"] = "1 ab März 2013 vorläufige Ergebnisse",
            ["A25"] = "2 Tätige Personen = Durchschnitt der Monate",
            ["A26"] = "3 Einschl. landwirtschaftl. Bau sowie Unternehmen der Bahn und Post."
        };

    private static readonly string[] ExpectedMergedRanges =
    [
        "A24:J24",
        "A25:J25",
        "A26:J26",
        "A6:B7",
        "E6:E7",
        "D6:D7",
        "C6:C7"
    ];

    public static XlsxStructureValidationReport ValidateTable1(
        XlsxTable1Extractor.ExtractionResult extraction)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        var checks = new List<XlsxStructureCheck>();

        void Check(
            string code,
            bool passed,
            XlsxStructureIssueSeverity severity,
            string message)
        {
            checks.Add(new XlsxStructureCheck(
                code,
                passed,
                severity,
                message));
        }

        var cells = extraction.Cells ?? [];
        var observations = extraction.Observations ?? [];
        var headerRows = extraction.HeaderRows ?? [];

        Check(
            "null-cell",
            !cells.Any(cell => cell is null),
            XlsxStructureIssueSeverity.Error,
            "The extracted cell collection must not contain null entries.");

        Check(
            "null-observation",
            !observations.Any(observation => observation is null),
            XlsxStructureIssueSeverity.Error,
            "The observation collection must not contain null entries.");

        Check(
            "null-header-row",
            !headerRows.Any(row => row is null),
            XlsxStructureIssueSeverity.Error,
            "The header row collection must not contain null entries.");

        Check(
            "worksheet-identity",
            extraction.WorksheetName == "tab.1 (S.7)" &&
            extraction.WorksheetPath == "xl/worksheets/sheet7.xml",
            XlsxStructureIssueSeverity.Error,
            "The expected Table 1 worksheet must be tab.1 (S.7) at xl/worksheets/sheet7.xml.");

        var validCells = cells
            .Where(cell => cell is not null)
            .ToList();

        var invalidReferences = validCells
            .Where(cell =>
                string.IsNullOrWhiteSpace(cell.Reference) ||
                !Regex.IsMatch(
                    cell.Reference,
                    @"^[A-Z]{1,3}[1-9][0-9]*$",
                    RegexOptions.CultureInvariant))
            .Select(cell => cell.Reference ?? "(null)")
            .ToList();

        Check(
            "cell-reference-format",
            invalidReferences.Count == 0,
            XlsxStructureIssueSeverity.Error,
            invalidReferences.Count == 0
                ? "All cell references have valid Excel A1 notation."
                : $"Invalid cell references: {string.Join(", ", invalidReferences)}.");

        var duplicateReferences = validCells
            .GroupBy(
                cell => cell.Reference ?? "",
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Check(
            "duplicate-cell-reference",
            duplicateReferences.Count == 0,
            XlsxStructureIssueSeverity.Error,
            duplicateReferences.Count == 0
                ? "All extracted cell references are unique."
                : $"Duplicate cell references: {string.Join(", ", duplicateReferences)}.");

        foreach (var expected in ExpectedCells)
        {
            var matches = validCells
                .Where(cell =>
                    string.Equals(
                        cell.Reference,
                        expected.Key,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();

            var passed = matches.Count == 1 &&
                Normalize(matches[0].ResolvedValue) ==
                Normalize(expected.Value);

            Check(
                "required-cell",
                passed,
                XlsxStructureIssueSeverity.Error,
                passed
                    ? $"Required cell {expected.Key} is present and correct."
                    : $"Required cell {expected.Key} is missing, duplicated, or differs from the expected label/value.");
        }

        var validHeaderRows = headerRows
            .Where(row => row is not null)
            .ToList();

        var duplicateHeaderRows = validHeaderRows
            .GroupBy(row => row.RowNumber)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Check(
            "duplicate-header-row",
            duplicateHeaderRows.Length == 0,
            XlsxStructureIssueSeverity.Error,
            duplicateHeaderRows.Length == 0
                ? "Header row numbers are unique."
                : $"Duplicate header rows: {string.Join(", ", duplicateHeaderRows)}.");

        var headerRowNumbers = validHeaderRows
            .Select(row => row.RowNumber)
            .ToHashSet();

        foreach (var requiredRow in new[] { 6, 7 })
        {
            Check(
                "header-row",
                headerRowNumbers.Contains(requiredRow),
                XlsxStructureIssueSeverity.Error,
                $"Header row {requiredRow} must be present.");
        }

        var unexpectedHeaderRows = headerRowNumbers
            .Except([6, 7])
            .ToArray();

        Check(
            "unexpected-header-row",
            unexpectedHeaderRows.Length == 0,
            XlsxStructureIssueSeverity.Warning,
            unexpectedHeaderRows.Length == 0
                ? "No additional header rows were found."
                : $"Additional header rows found: {string.Join(", ", unexpectedHeaderRows)}.");

        Check(
            "heading-count",
            extraction.Headings?.Count == 4,
            XlsxStructureIssueSeverity.Error,
            "The extracted heading list must contain four entries.");

        Check(
            "footnote-count",
            extraction.Footnotes?.Count == 3,
            XlsxStructureIssueSeverity.Error,
            "The extracted footnote list must contain three entries.");

        var headingReferences = new[] { "A1", "A2", "A3", "A4" };

        var headingsMatchCells =
            extraction.Headings is not null &&
            extraction.Headings.Count == headingReferences.Length &&
            headingReferences.Select((reference, index) =>
            {
                var matches = validCells
                    .Where(cell => string.Equals(
                        cell.Reference,
                        reference,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return matches.Count == 1 &&
                    Normalize(extraction.Headings[index]) ==
                    Normalize(matches[0].ResolvedValue);
            }).All(matches => matches);

        Check(
            "heading-metadata-consistency",
            headingsMatchCells,
            XlsxStructureIssueSeverity.Error,
            "The heading metadata must match source cells A1–A4 in order.");

        var footnoteReferences = new[] { "A24", "A25", "A26" };

        var footnotesMatchCells =
            extraction.Footnotes is not null &&
            extraction.Footnotes.Count == footnoteReferences.Length &&
            footnoteReferences.Select((reference, index) =>
            {
                var matches = validCells
                    .Where(cell => string.Equals(
                        cell.Reference,
                        reference,
                        StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return matches.Count == 1 &&
                    Normalize(extraction.Footnotes[index]) ==
                    Normalize(matches[0].ResolvedValue);
            }).All(matches => matches);

        Check(
            "footnote-metadata-consistency",
            footnotesMatchCells,
            XlsxStructureIssueSeverity.Error,
            "The footnote metadata must match source cells A24–A26 in order.");

        foreach (var column in ExpectedColumns)
        {
            var present =
                extraction.ColumnHeaders is not null &&
                extraction.ColumnHeaders.TryGetValue(column, out var header) &&
                !string.IsNullOrWhiteSpace(header);

            Check(
                "column-header",
                present,
                XlsxStructureIssueSeverity.Error,
                $"Column {column} must have a non-empty extracted header.");
        }

        var mergedRanges = extraction.MergedRanges ??
            [];

        foreach (var requiredRange in ExpectedMergedRanges)
        {
            Check(
                "merged-range",
                mergedRanges.Contains(
                    requiredRange,
                    StringComparer.OrdinalIgnoreCase),
                XlsxStructureIssueSeverity.Error,
                $"Required merged range {requiredRange} must exist.");
        }

        var unexpectedMergedRanges = mergedRanges
            .Where(range => !ExpectedMergedRanges.Contains(
                range,
                StringComparer.OrdinalIgnoreCase))
            .ToArray();

        Check(
            "unexpected-merged-range",
            unexpectedMergedRanges.Length == 0,
            XlsxStructureIssueSeverity.Warning,
            unexpectedMergedRanges.Length == 0
                ? "No additional merged ranges were found."
                : $"Additional merged ranges found: {string.Join(", ", unexpectedMergedRanges)}.");

        var expectedObservationKeys = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        var usableObservations = observations
            .Where(observation => observation is not null)
            .ToList();

        foreach (var row in ExpectedRows)
        {
            var parentRow = ParentRows[row];
            var indicator = ExpectedCells[$"A{parentRow}"];
            var category = row == parentRow
                ? null
                : ExpectedCells[$"A{row}"];

            var unit = ExpectedCells.TryGetValue(
                $"B{parentRow}",
                out var expectedUnit)
                ? expectedUnit
                : null;

            foreach (var column in ExpectedColumns)
            {
                var reference = $"{column}{row}";
                var key = $"{reference}|{column}";

                expectedObservationKeys.Add(key);

                var matches = usableObservations
                    .Where(observation =>
                        string.Equals(
                            observation.CellReference,
                            reference,
                            StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(
                            observation.Column,
                            column,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var existsExactlyOnce = matches.Count == 1;

                Check(
                    "expected-observation",
                    existsExactlyOnce,
                    XlsxStructureIssueSeverity.Error,
                    existsExactlyOnce
                        ? $"Observation {reference} exists exactly once."
                        : $"Expected exactly one observation for {reference}; found {matches.Count}.");

                if (!existsExactlyOnce)
                    continue;

                var observation = matches[0];

                var hierarchyCorrect =
                    observation.SourceRow == row &&
                    observation.IndicatorSourceRow == parentRow &&
                    Normalize(observation.Indicator) == Normalize(indicator) &&
                    Normalize(observation.Category) == Normalize(category) &&
                    Normalize(observation.Unit) == Normalize(unit);

                Check(
                    "observation-hierarchy",
                    hierarchyCorrect,
                    XlsxStructureIssueSeverity.Error,
                    hierarchyCorrect
                        ? $"Observation {reference} has the expected indicator, category, parent and unit."
                        : $"Observation {reference} has inconsistent indicator hierarchy, parent row, or unit.");

                var columnHeaderCorrect =
                    extraction.ColumnHeaders is not null &&
                    extraction.ColumnHeaders.TryGetValue(
                        column,
                        out var expectedHeader) &&
                    Normalize(observation.ColumnHeader) ==
                    Normalize(expectedHeader);

                Check(
                    "observation-column-header",
                    columnHeaderCorrect,
                    XlsxStructureIssueSeverity.Error,
                    $"Observation {reference} must retain its corresponding column header.");
            }
        }

        var duplicateObservationKeys = usableObservations
            .GroupBy(
                observation =>
                    $"{observation.CellReference}|{observation.Column}",
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Check(
            "duplicate-observation",
            duplicateObservationKeys.Length == 0,
            XlsxStructureIssueSeverity.Error,
            duplicateObservationKeys.Length == 0
                ? "Observation references are unique."
                : $"Duplicate observations: {string.Join(", ", duplicateObservationKeys)}.");

        var unexpectedObservations = usableObservations
            .Where(observation =>
                !expectedObservationKeys.Contains(
                    $"{observation.CellReference}|{observation.Column}"))
            .Select(observation =>
                $"{observation.CellReference}|{observation.Column}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Check(
            "unexpected-observation",
            unexpectedObservations.Length == 0,
            XlsxStructureIssueSeverity.Warning,
            unexpectedObservations.Length == 0
                ? "No additional observations were found."
                : $"Additional observations found: {string.Join(", ", unexpectedObservations)}.");

        Check(
            "observation-count",
            usableObservations.Count >= 112,
            XlsxStructureIssueSeverity.Error,
            usableObservations.Count < 112
                ? $"Expected at least 112 observations; found {usableObservations.Count}."
                : $"Found {usableObservations.Count} observations; the baseline contains 112.");

        var actualRows = usableObservations
            .Select(observation => observation.SourceRow)
            .Distinct()
            .ToHashSet();

        var missingRows = ExpectedRows
            .Where(row => !actualRows.Contains(row))
            .ToArray();

        Check(
            "observation-rows",
            missingRows.Length == 0,
            XlsxStructureIssueSeverity.Error,
            missingRows.Length == 0
                ? "All expected observation rows 9–22 are present."
                : $"Missing observation rows: {string.Join(", ", missingRows)}.");

        var unexpectedRows = actualRows
            .Except(ExpectedRows)
            .ToArray();

        Check(
            "unexpected-observation-row",
            unexpectedRows.Length == 0,
            XlsxStructureIssueSeverity.Warning,
            unexpectedRows.Length == 0
                ? "No additional observation rows were found."
                : $"Additional observation rows found: {string.Join(", ", unexpectedRows)}.");

        return new XlsxStructureValidationReport(checks);
    }

    private static string Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? ""
            : Regex.Replace(value.Trim(), @"\s+", " ");
    }
}
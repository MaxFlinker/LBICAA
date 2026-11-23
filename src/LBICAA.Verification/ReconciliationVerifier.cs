
using System.Globalization;

namespace LBICAA.Verification;

public enum ReconciliationStatus
{
    Passed,
    Failed,
    NotVerifiable
}

public sealed record ReconciliationObservation(
    string CellReference,
    string Column,
    string? ResolvedValue,
    string? DataType);
public sealed record ReconciliationRule(
    string Name,
    string Column,
    string ParentCellReference,
    IReadOnlyList<string> CategoryCellReferences,
    decimal Tolerance = 0m);

public sealed record ReconciliationResult(
    string RuleName,
    ReconciliationStatus Status,
    string ParentCellReference,
    IReadOnlyList<string> CategoryCellReferences,
    decimal? ParentValue,
    decimal? CategoryTotal,
    decimal? Difference,
    string Message);

public static class ReconciliationVerifier
{
    public static ReconciliationResult Verify(
        IEnumerable<ReconciliationObservation> observations,
        ReconciliationRule rule)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(rule);

        if (string.IsNullOrWhiteSpace(rule.Name))
            throw new ArgumentException("Rule name is required.", nameof(rule));

        if (string.IsNullOrWhiteSpace(rule.Column))
            throw new ArgumentException("Rule column is required.", nameof(rule));

        if (string.IsNullOrWhiteSpace(rule.ParentCellReference))
            throw new ArgumentException("Parent cell reference is required.", nameof(rule));

        if (rule.CategoryCellReferences is null ||
            rule.CategoryCellReferences.Count == 0)
        {
            throw new ArgumentException(
                "At least one category cell reference is required.",
                nameof(rule));
        }

        if (rule.Tolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(rule));

        var observationList = observations.ToList();

        if (!TryGetNumber(
                observationList,
                rule.ParentCellReference,
                rule.Column,
                out var parentValue,
                out var parentError))
        {
            return NotVerifiable(
                rule,
                null,
                null,
                null,
                parentError);
        }

        decimal categoryTotal = 0m;

        foreach (var categoryReference in rule.CategoryCellReferences)
        {
            if (!TryGetNumber(
                    observationList,
                    categoryReference,
                    rule.Column,
                    out var categoryValue,
                    out var categoryError))
            {
                return NotVerifiable(
                    rule,
                    parentValue,
                    null,
                    null,
                    categoryError);
            }

            categoryTotal += categoryValue;
        }

        var difference = parentValue - categoryTotal;

        var passed = Math.Abs(difference) <= rule.Tolerance;

        return new ReconciliationResult(
            rule.Name,
            passed
                ? ReconciliationStatus.Passed
                : ReconciliationStatus.Failed,
            rule.ParentCellReference,
            rule.CategoryCellReferences,
            parentValue,
            categoryTotal,
            difference,
            passed
                ? "Parent value reconciles with category total."
                : "Parent value does not reconcile with category total.");
    }

    private static bool TryGetNumber(
        IReadOnlyCollection<ReconciliationObservation> observations,
        string cellReference,
        string column,
        out decimal value,
        out string error)
    {
        value = 0m;
        error = "";

        var matches = observations
            .Where(item =>
                string.Equals(
                    item.CellReference,
                    cellReference,
                    StringComparison.OrdinalIgnoreCase) &&
                string.Equals(
                    item.Column,
                    column,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            error = $"Observation {cellReference} in column {column} is missing.";
            return false;
        }

        if (matches.Count > 1)
        {
            error = $"Observation {cellReference} in column {column} is duplicated.";
            return false;
        }

        var text = matches[0].ResolvedValue;
        var dataType = matches[0].DataType;

        if (dataType is not null &&
            !string.Equals(dataType, "n", StringComparison.OrdinalIgnoreCase))
        {
            error =
                $"Observation {cellReference} in column {column} " +
                "is stored as non-numeric data and cannot be verified.";

            return false;
        }

        if (string.IsNullOrWhiteSpace(text) ||
            !decimal.TryParse(
                text,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out value))
        {
            error =
                $"Observation {cellReference} in column {column} " +
                "does not contain a verifiable numeric value.";

            return false;
        }

        return true;
    }

    private static ReconciliationResult NotVerifiable(
        ReconciliationRule rule,
        decimal? parentValue,
        decimal? categoryTotal,
        decimal? difference,
        string message)
    {
        return new ReconciliationResult(
            rule.Name,
            ReconciliationStatus.NotVerifiable,
            rule.ParentCellReference,
            rule.CategoryCellReferences,
            parentValue,
            categoryTotal,
            difference,
            message);
    }
}

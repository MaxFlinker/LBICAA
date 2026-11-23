using LBICAA.Verification;

namespace LBICAA.Tests;

public class ReconciliationVerifierTests
{
    private static ReconciliationRule CreateRule(decimal tolerance = 0m)
    {
        return new ReconciliationRule(
            "Test reconciliation",
            "E",
            "E10",
            ["E11", "E12"],
            tolerance);
    }

    private static List<ReconciliationObservation> CreateObservations(
        string parentValue = "10",
        string firstCategoryValue = "4",
        string secondCategoryValue = "6",
        string? dataType = null)
    {
        return
        [
            new("E10", "E", parentValue, null),
            new("E11", "E", firstCategoryValue, null),
            new("E12", "E", secondCategoryValue, dataType)
        ];
    }

    [Fact]
    public void ReturnsPassedWhenParentEqualsCategoryTotal()
    {
        var result = ReconciliationVerifier.Verify(
            CreateObservations(),
            CreateRule());

        Assert.Equal(ReconciliationStatus.Passed, result.Status);
        Assert.Equal(10m, result.ParentValue);
        Assert.Equal(10m, result.CategoryTotal);
        Assert.Equal(0m, result.Difference);
    }

    [Fact]
    public void AcceptsExplicitNumericCellType()
    {
        var observations = CreateObservations();

        observations[0] = new("E10", "E", "10", "n");
        observations[1] = new("E11", "E", "4", "n");
        observations[2] = new("E12", "E", "6", "n");

        var result = ReconciliationVerifier.Verify(
            observations,
            CreateRule());

        Assert.Equal(ReconciliationStatus.Passed, result.Status);
    }

    [Fact]
    public void ReturnsFailedWhenParentDoesNotEqualCategoryTotal()
    {
        var result = ReconciliationVerifier.Verify(
            CreateObservations(parentValue: "11"),
            CreateRule());

        Assert.Equal(ReconciliationStatus.Failed, result.Status);
        Assert.Equal(11m, result.ParentValue);
        Assert.Equal(10m, result.CategoryTotal);
        Assert.Equal(1m, result.Difference);
    }

    [Fact]
    public void ReturnsNotVerifiableForTextualZeroMarker()
    {
        var result = ReconciliationVerifier.Verify(
            CreateObservations(secondCategoryValue: "0", dataType: "s"),
            CreateRule());

        Assert.Equal(ReconciliationStatus.NotVerifiable, result.Status);
        Assert.Equal(10m, result.ParentValue);
        Assert.Null(result.CategoryTotal);
        Assert.Null(result.Difference);
    }

    [Fact]
    public void ReturnsNotVerifiableWhenObservationIsMissing()
    {
        var observations = CreateObservations();
        observations.RemoveAt(2);

        var result = ReconciliationVerifier.Verify(
            observations,
            CreateRule());

        Assert.Equal(ReconciliationStatus.NotVerifiable, result.Status);
        Assert.Contains("missing", result.Message);
    }

    [Fact]
    public void ReturnsNotVerifiableWhenObservationIsDuplicated()
    {
        var observations = CreateObservations();
        observations.Add(new("E11", "E", "4", null));

        var result = ReconciliationVerifier.Verify(
            observations,
            CreateRule());

        Assert.Equal(ReconciliationStatus.NotVerifiable, result.Status);
        Assert.Contains("duplicated", result.Message);
    }

    [Fact]
    public void AppliesConfiguredTolerance()
    {
        var result = ReconciliationVerifier.Verify(
            CreateObservations(parentValue: "10.5"),
            CreateRule(tolerance: 0.5m));

        Assert.Equal(ReconciliationStatus.Passed, result.Status);
        Assert.Equal(0.5m, result.Difference);
    }

    [Fact]
    public void ReturnsNotVerifiableWhenParentObservationIsMissing()
    {
        var observations = CreateObservations();
        observations.RemoveAt(0);

        var result = ReconciliationVerifier.Verify(
            observations,
            CreateRule());

        Assert.Equal(ReconciliationStatus.NotVerifiable, result.Status);
        Assert.Null(result.ParentValue);
        Assert.Contains("missing", result.Message);
    }

    [Fact]
    public void ReturnsNotVerifiableWhenParentObservationIsDuplicated()
    {
        var observations = CreateObservations();
        observations.Add(new("E10", "E", "10", null));

        var result = ReconciliationVerifier.Verify(
            observations,
            CreateRule());

        Assert.Equal(ReconciliationStatus.NotVerifiable, result.Status);
        Assert.Contains("duplicated", result.Message);
    }

    [Fact]
    public void ReturnsNotVerifiableForNonNumericValue()
    {
        var result = ReconciliationVerifier.Verify(
            CreateObservations(secondCategoryValue: "unknown"),
            CreateRule());

        Assert.Equal(ReconciliationStatus.NotVerifiable, result.Status);
        Assert.Equal(10m, result.ParentValue);
        Assert.Contains("numeric", result.Message);
    }

    [Fact]
    public void ReturnsFailedWhenDifferenceExceedsTolerance()
    {
        var result = ReconciliationVerifier.Verify(
            CreateObservations(parentValue: "10.51"),
            CreateRule(tolerance: 0.5m));

        Assert.Equal(ReconciliationStatus.Failed, result.Status);
        Assert.Equal(0.51m, result.Difference);
    }

    [Fact]
    public void MatchesCellReferencesAndColumnsCaseInsensitively()
    {
        var observations = new List<ReconciliationObservation>
        {
            new("e10", "e", "10", null),
            new("e11", "e", "4", null),
            new("e12", "e", "6", null)
        };

        var result = ReconciliationVerifier.Verify(
            observations,
            CreateRule());

        Assert.Equal(ReconciliationStatus.Passed, result.Status);
    }
}

using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using Xunit;

namespace LBICAA.Tests;

public sealed class ApiWorkflowIntegrationTests
{
    [Fact]
    public async Task Full_api_workflow_creates_and_retrieves_claim_with_evidence_and_verification()
    {
        using var factory = new LbicaaApiFactory();
        using var client = factory.CreateClient();

        factory.InitializeDatabase();

        // 1. Create a source.
        using var sourceResponse = await client.PostAsJsonAsync(
            "/sources",
            new
            {
                title = "Integration Test Source",
                publisher = "LBICAA Test Suite",
                uri = "https://example.org/test-source",
                type = "OfficialDocument",
                licenceStatus = "Unknown"
            });

        using var sourceJson = await ReadJsonAsync(sourceResponse);
        var sourceId = sourceJson.RootElement
            .GetProperty("id")
            .GetGuid();

        // 2. Create a claim.
        using var claimResponse = await client.PostAsJsonAsync(
            "/claims",
            new
            {
                subjectId = "integration-test:subject-001",
                predicate = "hasStatus",
                @object = new
                {
                    kind = "EntityReference",
                    value = "integration-test:status-001"
                },
                nature = "Documented",
                confidence = "High",
                validity = new
                {
                    validFrom = (DateOnly?)null,
                    validUntil = (DateOnly?)null
                }
            });

        using var claimJson = await ReadJsonAsync(claimResponse);
        var claimId = claimJson.RootElement
            .GetProperty("id")
            .GetGuid();

        // A claim may exist before any evidence or verification is recorded.
        using (var emptyHistoryResponse =
            await client.GetAsync($"/claims/{claimId}"))
        using (var emptyHistoryJson =
            await ReadJsonAsync(emptyHistoryResponse))
        {
            var emptyDetails = emptyHistoryJson.RootElement;

            Assert.Empty(
                emptyDetails.GetProperty("evidence").EnumerateArray());

            Assert.Empty(
                emptyDetails.GetProperty("verificationHistory").EnumerateArray());
        }

        // 3. Attach evidence to the claim.
        using var evidenceResponse = await client.PostAsJsonAsync(
            $"/claims/{claimId}/evidence",
            new
            {
                sourceId,
                locator = "page:1",
                assessment = "Supports",
                excerpt = "Integration test evidence."
            });

        using var evidenceJson = await ReadJsonAsync(evidenceResponse);

        // Attach contradicting evidence to the same claim.
        using var secondEvidenceResponse = await client.PostAsJsonAsync(
            $"/claims/{claimId}/evidence",
            new
            {
                sourceId,
                locator = "page:2",
                assessment = "Contradicts",
                excerpt = "Contradicting integration test evidence."
            });

        using var secondEvidenceJson =
            await ReadJsonAsync(secondEvidenceResponse);

        Assert.Equal(
            sourceId,
            evidenceJson.RootElement.GetProperty("sourceId").GetGuid());

        // 4. Record a verification.
        using var verificationResponse = await client.PostAsJsonAsync(
            $"/claims/{claimId}/verifications",
            new
            {
                method = "ManualReview",
                outcome = "Confirmed",
                verifierReference = "integration-test:verifier-001",
                notes = "Confirmed by the integration test."
            });

        using var verificationJson = await ReadJsonAsync(verificationResponse);

       // Record a second verification without replacing the first.
        using var secondVerificationResponse = await client.PostAsJsonAsync(
            $"/claims/{claimId}/verifications",
            new
            {
                method = "CrossSourceComparison",
                outcome = "Disputed",
                verifierReference = "integration-test:verifier-002",
                notes = "Second verification with a different outcome."
            });

        using var secondVerificationJson =
            await ReadJsonAsync(secondVerificationResponse);

        Assert.Equal(
            "Confirmed",
            verificationJson.RootElement.GetProperty("outcome").GetString());

        // 5. Retrieve the claim and verify its complete history.
        using var getResponse = await client.GetAsync($"/claims/{claimId}");
        using var claimDetailsJson = await ReadJsonAsync(getResponse);

        var details = claimDetailsJson.RootElement;

        Assert.Equal(claimId, details.GetProperty("id").GetGuid());
        Assert.Equal(
            "integration-test:subject-001",
            details.GetProperty("subjectId").GetString());

        Assert.Equal(
            "hasStatus",
            details.GetProperty("predicate").GetString());

        Assert.Equal(
            "integration-test:status-001",
            details.GetProperty("object").GetProperty("value").GetString());

        var evidence = details.GetProperty("evidence");
        Assert.Equal(2, evidence.GetArrayLength());

        var assessments = evidence.EnumerateArray()
            .Select(item => item.GetProperty("assessment").GetString())
            .ToArray();

        Assert.Contains("Supports", assessments);
        Assert.Contains("Contradicts", assessments);
        Assert.Equal(
            sourceId,
            evidence[0].GetProperty("sourceId").GetGuid());
        Assert.Equal(
            "Supports",
            evidence[0].GetProperty("assessment").GetString());

        var verificationHistory = details.GetProperty("verificationHistory");
       Assert.Equal(2, verificationHistory.GetArrayLength());

        var outcomes = verificationHistory.EnumerateArray()
            .Select(item => item.GetProperty("outcome").GetString())
            .ToArray();

        var verifierReferences = verificationHistory.EnumerateArray()
            .Select(item => item.GetProperty("verifierReference").GetString())
            .ToArray();

        Assert.Contains("Confirmed", outcomes);
        Assert.Contains("Disputed", outcomes);

        Assert.Contains("integration-test:verifier-001", verifierReferences);
        Assert.Contains("integration-test:verifier-002", verifierReferences);
        
        Assert.Equal(
            "ManualReview",
            verificationHistory[0].GetProperty("method").GetString());
        Assert.Equal(
            "Confirmed",
            verificationHistory[0].GetProperty("outcome").GetString());
        Assert.Equal(
            "integration-test:verifier-001",
            verificationHistory[0].GetProperty("verifierReference").GetString());
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        Assert.True(
            response.IsSuccessStatusCode,
            $"Expected a successful HTTP response, but received " +
            $"{(int)response.StatusCode} ({response.StatusCode}). " +
            $"Response body: {content}");

        return JsonDocument.Parse(content);
    }
}

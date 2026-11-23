
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace LBICAA.Tests;

public sealed class ApiErrorHandlingIntegrationTests
{
    [Fact]
    public async Task Requests_for_nonexistent_claims_return_not_found()
    {
        using var factory = new LbicaaApiFactory();
        using var client = factory.CreateClient();

        factory.InitializeDatabase();

                // Create a valid source so the evidence request tests
        // only the nonexistent claim condition.
        using var sourceResponse = await client.PostAsJsonAsync(
            "/sources",
            new
            {
                title = "Valid test source",
                publisher = "LBICAA Test Suite",
                uri = "https://example.org/valid-source",
                type = "OfficialDocument",
                licenceStatus = "Unknown"
            });

        Assert.Equal(HttpStatusCode.Created, sourceResponse.StatusCode);

        using var sourceJson = await sourceResponse.Content
            .ReadFromJsonAsync<JsonDocument>();

        var sourceId = sourceJson!.RootElement
            .GetProperty("id")
            .GetGuid();

        var nonexistentClaimId = Guid.NewGuid();

        // GET a claim that does not exist.
        using var getResponse = await client.GetAsync(
            $"/claims/{nonexistentClaimId}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        // Attach evidence to a claim that does not exist.
        using var evidenceResponse = await client.PostAsJsonAsync(
            $"/claims/{nonexistentClaimId}/evidence",
            new
            {
                sourceId = sourceId,
                locator = "page:1",
                assessment = "Supports",
                excerpt = "Evidence for a nonexistent claim."
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            evidenceResponse.StatusCode);

        // Record verification for a claim that does not exist.
        using var verificationResponse = await client.PostAsJsonAsync(
            $"/claims/{nonexistentClaimId}/verifications",
            new
            {
                method = "ManualReview",
                outcome = "Confirmed",
                verifierReference = "integration-test:missing-claim",
                notes = "Verification for a nonexistent claim."
            });

        Assert.Equal(
            HttpStatusCode.NotFound,
            verificationResponse.StatusCode);
    }

    [Fact]
    public async Task Creating_a_source_with_invalid_required_fields_returns_bad_request()
    {
        using var factory = new LbicaaApiFactory();
        using var client = factory.CreateClient();

        factory.InitializeDatabase();

        using var response = await client.PostAsJsonAsync(
            "/sources",
            new
            {
                title = "",
                publisher = "",
                uri = "https://example.org/invalid-source",
                type = "OfficialDocument",
                licenceStatus = "Unknown"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

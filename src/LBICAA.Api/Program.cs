using System.Text.Json.Serialization;
using LBICAA.Api.Contracts.Claims;
using LBICAA.Api.Contracts.Evidence;
using LBICAA.Domain.Evidence;
using LBICAA.Application.Claims.CreateClaim;
using LBICAA.Application.Claims.GetClaimDetails;
using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Application.Evidence.AttachEvidence;
using LBICAA.Application.Verification.RecordVerification;
using LBICAA.Domain.Claims;
using LBICAA.Domain.Sources;
using LBICAA.Infrastructure.Persistence;
using LBICAA.Infrastructure.Persistence.Repositories;
using LBICAA.Application.Sources.CreateSource;
using LBICAA.Api.Contracts.Sources;
using LBICAA.Api.Contracts.Verification;
using LBICAA.Domain.Verification;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

builder.Services.AddDbContext<LbicaaDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Lbicaa")
        ?? "Data Source=lbicaa.db"));

// Persistence
builder.Services.AddScoped<IClaimRepository, EfClaimRepository>();
builder.Services.AddScoped<ISourceRepository, EfSourceRepository>();
builder.Services.AddScoped<IEvidenceRepository, EfEvidenceRepository>();
builder.Services.AddScoped<IVerificationRepository, EfVerificationRepository>();

// Application use cases
builder.Services.AddScoped<CreateClaimHandler>();
builder.Services.AddScoped<CreateSourceHandler>();
builder.Services.AddScoped<GetClaimDetailsHandler>();
builder.Services.AddScoped<AttachEvidenceHandler>();
builder.Services.AddScoped<RecordVerificationHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


// POST /sources
app.MapPost("/sources",
    async Task<IResult> (
        CreateSourceApiRequest request,
        CreateSourceHandler handler,
        CancellationToken cancellationToken) =>
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Source title is required."];

        if (string.IsNullOrWhiteSpace(request.Publisher))
            errors["publisher"] = ["Publisher is required."];

        if (request.Uri is null ||
            !request.Uri.IsAbsoluteUri ||
            (request.Uri.Scheme != Uri.UriSchemeHttp &&
             request.Uri.Scheme != Uri.UriSchemeHttps))
            errors["uri"] = ["A valid HTTP(S) URI is required."];

        if (!Enum.IsDefined(typeof(SourceType), request.Type))
            errors["type"] = ["Invalid source type."];

        if (!Enum.IsDefined(typeof(LicenceStatus), request.LicenceStatus))
            errors["licenceStatus"] = ["Invalid licence status."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var createRequest = new CreateSourceRequest(
                request.Title,
                request.Publisher,
                request.Uri!,
                request.Type,
                request.LicenceStatus,
                request.PublishedOn,
                request.LicenceReference,
                request.ContentHash);

            var source = await handler.HandleAsync(
                createRequest,
                cancellationToken);

            var response = new SourceApiResponse(
                source.Id,
                source.Title,
                source.Publisher,
                source.Uri.ToString(),
                source.Type.ToString(),
                source.PublishedOn,
                source.RetrievedAt,
                source.LicenceStatus.ToString(),
                source.LicenceReference,
                source.ContentHash);

            return Results.Created(
                $"/sources/{source.Id}",
                response);
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["request"] = [exception.Message]
                });
        }
    })
    .WithName("CreateSource")
    .WithSummary("Register a source");

// POST /claims
app.MapPost("/claims",
    async Task<IResult> (
        CreateClaimApiRequest request,
        CreateClaimHandler handler,
        CancellationToken cancellationToken) =>
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.SubjectId))
            errors["subjectId"] = ["Subject ID is required."];

        if (string.IsNullOrWhiteSpace(request.Predicate))
            errors["predicate"] = ["Predicate is required."];

        if (request.Object is null)
            errors["object"] = ["Claim object is required."];

        if (request.Validity is null)
            errors["validity"] = ["Temporal validity is required."];

        if (!Enum.IsDefined(typeof(ClaimNature), request.Nature))
            errors["nature"] = ["Invalid claim nature."];

        if (!Enum.IsDefined(typeof(ConfidenceLevel), request.Confidence))
            errors["confidence"] = ["Invalid confidence level."];

        if (request.Object is not null)
        {
            if (!Enum.IsDefined(typeof(ClaimValueKind), request.Object.Kind))
                errors["object.kind"] = ["Invalid claim value kind."];

            if (string.IsNullOrWhiteSpace(request.Object.Value))
                errors["object.value"] = ["Claim value is required."];
        }

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        if (request.Object is null || request.Validity is null)
            return Results.ValidationProblem(errors);

        ClaimValue claimValue;

        try
        {
            claimValue = request.Object.Kind switch
            {
                ClaimValueKind.EntityReference =>
                    ClaimValue.Entity(request.Object.Value),

                ClaimValueKind.Literal =>
                    ClaimValue.Literal(request.Object.Value),

                _ => throw new ArgumentException("Invalid claim value kind.")
            };

            var validity = new TemporalValidity(
                request.Validity.ValidFrom,
                request.Validity.ValidUntil);

            var createRequest = new CreateClaimRequest(
                request.SubjectId,
                request.Predicate,
                claimValue,
                request.Nature,
                request.Confidence,
                validity);

            var claim = await handler.HandleAsync(
                createRequest,
                cancellationToken);

            return Results.Created(
                $"/claims/{claim.Id}",
                new
                {
                    claim.Id,
                    claim.SubjectId,
                    claim.Predicate,
                    Object = new ClaimValueApiResponse(
                        claim.Object.Kind.ToString(),
                        claim.Object.Value),
                    Nature = claim.Nature.ToString(),
                    Status = claim.Status.ToString(),
                    Confidence = claim.Confidence.ToString(),
                    Validity = new TemporalValidityApiResponse(
                        claim.Validity.ValidFrom,
                        claim.Validity.ValidUntil),
                    claim.RecordedAt
                });
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["request"] = [exception.Message]
                });
        }
    })
    .WithName("CreateClaim")
    .WithSummary("Create a claim");

// GET /claims/{id}
app.MapGet("/claims/{id:guid}",
    async Task<IResult> (
        Guid id,
        GetClaimDetailsHandler handler,
        CancellationToken cancellationToken) =>
    {
        var details = await handler.HandleAsync(
            new GetClaimDetailsRequest(id),
            cancellationToken);

        if (details is null)
            return Results.NotFound();

        var claim = details.Claim;

        var response = new ClaimDetailsApiResponse(
            claim.Id,
            claim.SubjectId,
            claim.Predicate,
            new ClaimValueApiResponse(
                claim.Object.Kind.ToString(),
                claim.Object.Value),
            claim.Nature.ToString(),
            claim.Status.ToString(),
            claim.Confidence.ToString(),
            new TemporalValidityApiResponse(
                claim.Validity.ValidFrom,
                claim.Validity.ValidUntil),
            claim.RecordedAt,
            details.Evidence.Select(evidence =>
                new EvidenceApiResponse(
                    evidence.Id,
                    evidence.SourceId,
                    evidence.Locator,
                    evidence.Excerpt,
                    evidence.Assessment.ToString(),
                    evidence.RecordedAt))
                .ToList(),
            details.VerificationHistory.Select(record =>
                new VerificationApiResponse(
                    record.Id,
                    record.Method.ToString(),
                    record.Outcome.ToString(),
                    record.VerifierReference,
                    record.Notes,
                    record.VerifiedAt))
                .ToList());

        return Results.Ok(response);
    })
    .WithName("GetClaimById")
    .WithSummary("Get a claim with its evidence and verification history");
    // POST /claims/{id}/evidence
app.MapPost("/claims/{id:guid}/evidence",
    async Task<IResult> (
        Guid id,
        AttachEvidenceApiRequest request,
        AttachEvidenceHandler handler,
        CancellationToken cancellationToken) =>
    {
        var errors = new Dictionary<string, string[]>();

        if (request.SourceId == Guid.Empty)
            errors["sourceId"] = ["A valid source ID is required."];

        if (string.IsNullOrWhiteSpace(request.Locator))
            errors["locator"] = ["Evidence locator is required."];

        if (!Enum.IsDefined(typeof(EvidenceAssessment), request.Assessment))
            errors["assessment"] = ["Invalid evidence assessment."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var evidence = await handler.HandleAsync(
                new AttachEvidenceRequest(
                    id,
                    request.SourceId,
                    request.Locator,
                    request.Assessment,
                    request.Excerpt),
                cancellationToken);

            var response = new EvidenceApiResponse(
                evidence.Id,
                evidence.SourceId,
                evidence.Locator,
                evidence.Excerpt,
                evidence.Assessment.ToString(),
                evidence.RecordedAt);

            return Results.Created(
                $"/claims/{id}",
                response);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["request"] = [exception.Message]
                });
        }
    })
    .WithName("AttachEvidence")
    .WithSummary("Attach evidence to a claim");
    // POST /claims/{id}/verifications
app.MapPost("/claims/{id:guid}/verifications",
    async Task<IResult> (
        Guid id,
        RecordVerificationApiRequest request,
        RecordVerificationHandler handler,
        CancellationToken cancellationToken) =>
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.VerifierReference))
            errors["verifierReference"] =
                ["Verifier reference is required."];

        if (!Enum.IsDefined(typeof(VerificationMethod), request.Method))
            errors["method"] = ["Invalid verification method."];

        if (!Enum.IsDefined(typeof(VerificationOutcome), request.Outcome))
            errors["outcome"] = ["Invalid verification outcome."];

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        try
        {
            var record = await handler.HandleAsync(
                new RecordVerificationRequest(
                    id,
                    request.Method,
                    request.Outcome,
                    request.VerifierReference,
                    request.Notes),
                cancellationToken);

            var response = new VerificationApiResponse(
                record.Id,
                record.Method.ToString(),
                record.Outcome.ToString(),
                record.VerifierReference,
                record.Notes,
                record.VerifiedAt);

            return Results.Created(
                $"/claims/{id}",
                response);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException exception)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["request"] = [exception.Message]
                });
        }
    })
    .WithName("RecordVerification")
    .WithSummary("Record a verification result for a claim");
app.Run();

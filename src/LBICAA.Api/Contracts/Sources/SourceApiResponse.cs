
namespace LBICAA.Api.Contracts.Sources;

public sealed record SourceApiResponse(
    Guid Id,
    string Title,
    string Publisher,
    string Uri,
    string Type,
    DateOnly? PublishedOn,
    DateTimeOffset RetrievedAt,
    string LicenceStatus,
    string? LicenceReference,
    string? ContentHash);

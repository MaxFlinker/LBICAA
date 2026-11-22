
using LBICAA.Domain.Sources;

namespace LBICAA.Api.Contracts.Sources;

public sealed record CreateSourceApiRequest(
    string Title,
    string Publisher,
    Uri Uri,
    SourceType Type,
    LicenceStatus LicenceStatus,
    DateOnly? PublishedOn = null,
    string? LicenceReference = null,
    string? ContentHash = null);

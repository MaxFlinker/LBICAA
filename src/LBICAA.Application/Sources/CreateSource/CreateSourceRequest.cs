
using LBICAA.Domain.Sources;

namespace LBICAA.Application.Sources.CreateSource;

public sealed record CreateSourceRequest(
    string Title,
    string Publisher,
    Uri Uri,
    SourceType Type,
    LicenceStatus LicenceStatus,
    DateOnly? PublishedOn = null,
    string? LicenceReference = null,
    string? ContentHash = null);

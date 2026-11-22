
using LBICAA.Application.Abstractions.Persistence;
using LBICAA.Domain.Sources;

namespace LBICAA.Application.Sources.CreateSource;

public sealed class CreateSourceHandler(
    ISourceRepository sourceRepository)
{
    public async Task<Source> HandleAsync(
        CreateSourceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var source = new Source(
            id: Guid.NewGuid(),
            title: request.Title,
            publisher: request.Publisher,
            uri: request.Uri,
            type: request.Type,
            retrievedAt: DateTimeOffset.UtcNow,
            licenceStatus: request.LicenceStatus,
            publishedOn: request.PublishedOn,
            licenceReference: request.LicenceReference,
            contentHash: request.ContentHash);

        await sourceRepository.AddAsync(
            source,
            cancellationToken);

        return source;
    }
}
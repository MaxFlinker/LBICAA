
namespace LBICAA.Domain.Sources;

public enum SourceType
{
    Legislation,
    OfficialDocument,
    Dataset,
    OfficialWebsite,
    Publication,
    Archive,
    Other
}

public enum LicenceStatus
{
    Known,
    Unknown,
    Restricted,
    PublicDomain
}

public sealed record Source
{
    public Guid Id { get; }
    public string Title { get; }
    public string Publisher { get; }
    public Uri Uri { get; }
    public SourceType Type { get; }

    public DateOnly? PublishedOn { get; }
    public DateTimeOffset RetrievedAt { get; }

    public LicenceStatus LicenceStatus { get; }
    public string? LicenceReference { get; }

    public string? ContentHash { get; }

    public Source(
        Guid id,
        string title,
        string publisher,
        Uri uri,
        SourceType type,
        DateTimeOffset retrievedAt,
        LicenceStatus licenceStatus,
        DateOnly? publishedOn = null,
        string? licenceReference = null,
        string? contentHash = null)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Source ID is required.");

        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Source title is required.");

        if (string.IsNullOrWhiteSpace(publisher))
            throw new ArgumentException("Publisher is required.");

        if (uri is null || !uri.IsAbsoluteUri ||
            (uri.Scheme != Uri.UriSchemeHttp &&
             uri.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("A valid HTTP(S) URI is required.");

        Id = id;
        Title = title;
        Publisher = publisher;
        Uri = uri;
        Type = type;
        PublishedOn = publishedOn;
        RetrievedAt = retrievedAt;
        LicenceStatus = licenceStatus;
        LicenceReference = licenceReference;
        ContentHash = contentHash;
    }
}

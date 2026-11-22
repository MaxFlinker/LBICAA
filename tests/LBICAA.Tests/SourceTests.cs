using LBICAA.Domain.Sources;

namespace LBICAA.Tests;

public class SourceTests
{
    private static Source CreateSource(
        string title = "Hamburg Organisation Directory",
        Uri? uri = null,
        LicenceStatus licence = LicenceStatus.Known)
    {
        return new Source(
            Guid.NewGuid(),
            title,
            "Freie und Hansestadt Hamburg",
            uri ?? new Uri("https://www.hamburg.de/"),
            SourceType.OfficialWebsite,
            DateTimeOffset.UtcNow,
            licence);
    }

    [Fact]
    public void EmptyTitle_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateSource(title: ""));
    }

    [Fact]
    public void InvalidUri_IsRejected()
    {
        Assert.Throws<ArgumentException>(() =>
            CreateSource(uri: new Uri("file:///invalid.txt")));
    }

    [Fact]
    public void UnknownLicence_IsAcceptedAndRecorded()
    {
        var source = CreateSource(licence: LicenceStatus.Unknown);

        Assert.Equal(LicenceStatus.Unknown, source.LicenceStatus);
    }
}

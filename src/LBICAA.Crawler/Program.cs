
using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

namespace LBICAA.Crawler;

internal static class Program
{
    private const int MaxAttempts = 4;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        if (args.Length > 0 &&
            args[0].Equals("extract-table1", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length != 3)
            {
                Console.Error.WriteLine(
                    "Usage: LBICAA.Crawler extract-table1 <xlsx-path> <output-json-path>");

                return 1;
            }

            var xlsxPath = Path.GetFullPath(args[1]);
            var outputPath = Path.GetFullPath(args[2]);

            if (!File.Exists(xlsxPath))
            {
                Console.Error.WriteLine($"XLSX file not found: {xlsxPath}");
                return 1;
            }

            try
            {
                Console.WriteLine("LBICAA XLSX table extraction");
                Console.WriteLine($"Source: {xlsxPath}");
                Console.WriteLine($"Output: {outputPath}");

                XlsxTable1Extractor.Extract(xlsxPath, outputPath);

                Console.WriteLine("Extraction complete.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine($"Extraction failed: {exception}");
                return 1;
            }
        }
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };

        var cataloguePath = Path.GetFullPath(
            args.Length > 0
                ? args[0]
                : Path.Combine("data", "catalogue", "ckan-catalogue.json"));

        var archiveRoot = Path.GetFullPath(
            args.Length > 1
                ? args[1]
                : Path.Combine("data", "sources"));

        if (!File.Exists(cataloguePath))
        {
            Console.Error.WriteLine(
                $"Catalogue not found: {cataloguePath}");

            return 1;
        }

        Directory.CreateDirectory(archiveRoot);

        var artifactRoot = Path.Combine(
            archiveRoot, "artifacts", "sha256");

        var manifestRoot = Path.Combine(
            archiveRoot, "manifests");

        Directory.CreateDirectory(artifactRoot);
        Directory.CreateDirectory(manifestRoot);

        using var client = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(10)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "LBICAA/1.0 (source acquisition)");

        try
        {
            Console.WriteLine("LBICAA resource acquisition");
            Console.WriteLine($"Catalogue: {cataloguePath}");
            Console.WriteLine($"Archive:   {archiveRoot}");
            Console.WriteLine();

            var packages = await LoadCatalogueAsync(
                cataloguePath,
                cancellation.Token);

            var resources = packages
                .SelectMany(package => package.Resources)
                .ToList();

            var validResources = resources
                .Where(resource =>
                    Uri.TryCreate(
                        resource.Url,
                        UriKind.Absolute,
                        out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp
                        || uri.Scheme == Uri.UriSchemeHttps))
                .ToList();

            var invalidResources = resources
                .Except(validResources)
                .ToList();

            var groups = validResources
                .GroupBy(resource => resource.Url!,
                    StringComparer.Ordinal)
                .ToList();

            Console.WriteLine($"Packages:           {packages.Count}");
            Console.WriteLine($"Resource records:   {resources.Count}");
            Console.WriteLine($"Distinct URLs:      {groups.Count}");
            Console.WriteLine($"Invalid URLs:       {invalidResources.Count}");
            Console.WriteLine();

            var artifacts = new List<ArtifactManifest>();
            var artifactByUrl = new Dictionary<string, ArtifactManifest>(
                StringComparer.Ordinal);

            var groupNumber = 0;

            foreach (var group in groups)
            {
                cancellation.Token.ThrowIfCancellationRequested();

                groupNumber++;

                var expectedSizes = group
                    .Where(resource => resource.ExpectedSize.HasValue)
                    .Select(resource => resource.ExpectedSize!.Value)
                    .Distinct()
                    .ToList();

                Console.WriteLine(
                    $"[{groupNumber}/{groups.Count}] {group.Key}");

                Console.WriteLine(
                    $"  Referenced by {group.Count()} resource record(s)");

                var artifact = await AcquireArtifactAsync(
                    client,
                    artifactRoot,
                    group.Key,
                    expectedSizes,
                    cancellation.Token);

                artifacts.Add(artifact);
                artifactByUrl[group.Key] = artifact;

                Console.WriteLine(
                    $"  Status: {artifact.Status}");

                if (artifact.Sha256 is not null)
                {
                    Console.WriteLine(
                        $"  SHA-256: {artifact.Sha256}");

                    Console.WriteLine(
                        $"  Actual size: {artifact.ActualSizeBytes} bytes");
                }

                if (artifact.SizeMismatch)
                {
                    Console.WriteLine(
                        "  WARNING: CKAN size differs from actual size.");
                }

                if (artifact.Error is not null)
                {
                    Console.WriteLine(
                        $"  ERROR: {artifact.Error}");
                }

                Console.WriteLine();
            }

            var packageManifests = packages
                .Select(package => new PackageManifest(
                    package.Id,
                    package.Name,
                    package.Title,
                    package.Metadata,
                    package.Resources.Select(resource =>
                    {
                        ArtifactManifest? artifact = null;

                        if (resource.Url is not null)
                            artifactByUrl.TryGetValue(
                                resource.Url, out artifact);

                        return new ResourceReference(
                            resource.Id,
                            resource.Name,
                            resource.Url,
                            resource.ExpectedSize,
                            resource.Metadata,
                            artifact?.Sha256,
                            artifact?.Status
                                ?? "InvalidOrUnsupportedUrl");
                    }).ToList()))
                .ToList();

            var manifest = new AcquisitionManifest(
                "2.0",
                DateTimeOffset.UtcNow,
                cataloguePath,
                packages.Count,
                resources.Count,
                groups.Count,
                invalidResources.Count,
                packageManifests,
                artifacts);

            var manifestName =
                $"acquisition-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";

            var manifestPath = Path.Combine(
                manifestRoot, manifestName);

            await File.WriteAllTextAsync(
                manifestPath,
                JsonSerializer.Serialize(manifest, JsonOptions),
                cancellation.Token);

            var successful = artifacts.Count(x =>
                x.Status == "Downloaded"
                || x.Status == "AlreadyPresent");

            var failed = artifacts.Count(x =>
                x.Status == "Failed");

            var mismatchCount = artifacts.Count(x =>
                x.SizeMismatch);

            Console.WriteLine("ACQUISITION COMPLETE");
            Console.WriteLine("--------------------");
            Console.WriteLine($"Packages:          {packages.Count}");
            Console.WriteLine($"Resource records:  {resources.Count}");
            Console.WriteLine($"Unique URLs:       {groups.Count}");
            Console.WriteLine($"Successful:        {successful}");
            Console.WriteLine($"Failed:            {failed}");
            Console.WriteLine($"Size mismatches:   {mismatchCount}");
            Console.WriteLine($"Invalid URLs:      {invalidResources.Count}");
            Console.WriteLine($"Manifest:          {manifestPath}");

            return failed > 0 || invalidResources.Count > 0 ? 2 : 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine();
            Console.WriteLine("Acquisition cancelled.");

            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Acquisition failed: {exception}");

            return 1;
        }
    }

    private static async Task<List<CataloguePackage>>
        LoadCatalogueAsync(
            string path,
            CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);

        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(
                "The catalogue JSON must contain an array of CKAN packages.");
        }

        var packages = new List<CataloguePackage>();

        foreach (var item in document.RootElement.EnumerateArray())
        {
            var id = GetString(item, "id");

            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException(
                    "A catalogue package has no ID.");

            var packageResources = new List<CatalogueResource>();

            if (item.TryGetProperty("resources", out var resourceArray)
                && resourceArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var resource in resourceArray.EnumerateArray())
                {
                    var resourceId =
                        GetString(resource, "id") ?? "unknown";

                    packageResources.Add(new CatalogueResource(
                        resourceId,
                        GetString(resource, "name"),
                        GetString(resource, "url"),
                        GetLong(resource, "size"),
                        resource.Clone()));
                }
            }

            packages.Add(new CataloguePackage(
                id,
                GetString(item, "name"),
                GetString(item, "title"),
                item.Clone(),
                packageResources));
        }

        return packages;
    }

    private static async Task<ArtifactManifest>
        AcquireArtifactAsync(
            HttpClient client,
            string artifactRoot,
            string url,
            List<long> expectedSizes,
            CancellationToken cancellationToken)
    {
        var retrievedAt = DateTimeOffset.UtcNow;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var temporaryPath = Path.Combine(
                Path.GetTempPath(),
                $"lbicaa-{Guid.NewGuid():N}.partial");

            try
            {
                using var response = await client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var effectiveUrl =
                    response.RequestMessage?.RequestUri?.ToString();

                await using var input =
                    await response.Content.ReadAsStreamAsync(
                        cancellationToken);

                using var hash = IncrementalHash.CreateHash(
                    HashAlgorithmName.SHA256);

                long actualSize = 0;

                await using (var output = new FileStream(
                    temporaryPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true))
                {
                    var buffer = new byte[81920];

                    while (true)
                    {
                        var read = await input.ReadAsync(
                            buffer.AsMemory(),
                            cancellationToken);

                        if (read == 0)
                            break;

                        await output.WriteAsync(
                            buffer.AsMemory(0, read),
                            cancellationToken);

                        hash.AppendData(buffer, 0, read);
                        actualSize += read;
                    }

                    await output.FlushAsync(cancellationToken);
                }

                var sha256 = Convert.ToHexString(
                    hash.GetHashAndReset()).ToLowerInvariant();

                var directory = Path.Combine(
                    artifactRoot, sha256[..2]);

                Directory.CreateDirectory(directory);

                var artifactPath = Path.Combine(
                    directory, sha256);

                var alreadyPresent = File.Exists(artifactPath);

                if (alreadyPresent)
                {
                    File.Delete(temporaryPath);
                }
                else
                {
                    File.Move(temporaryPath, artifactPath);
                }

                var mismatch = expectedSizes.Any(
                    expected => expected != actualSize);

                return new ArtifactManifest(
                    url,
                    effectiveUrl,
                    sha256,
                    artifactPath,
                    actualSize,
                    expectedSizes,
                    mismatch,
                    response.StatusCode.ToString(),
                    response.Content.Headers.ContentType?.ToString(),
                    alreadyPresent ? "AlreadyPresent" : "Downloaded",
                    null,
                    retrievedAt);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is HttpRequestException
                || exception is IOException
                || exception is TaskCanceledException)
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                if (attempt == MaxAttempts)
                {
                    return new ArtifactManifest(
                        url,
                        null,
                        null,
                        null,
                        null,
                        expectedSizes,
                        false,
                        null,
                        null,
                        "Failed",
                        exception.ToString(),
                        retrievedAt);
                }

                var delaySeconds = (int)Math.Pow(2, attempt);

                Console.WriteLine(
                    $"  Attempt {attempt} failed: {exception.Message}");

                Console.WriteLine(
                    $"  Retrying in {delaySeconds} seconds...");

                await Task.Delay(
                    TimeSpan.FromSeconds(delaySeconds),
                    cancellationToken);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
        }

        throw new InvalidOperationException(
            "Unexpected end of acquisition retry loop.");
    }

    private static string? GetString(
        JsonElement element,
        string property)
    {
        return element.TryGetProperty(property, out var value)
               && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static long? GetLong(
        JsonElement element,
        string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number
            && value.TryGetInt64(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String
            && long.TryParse(value.GetString(), out number))
            return number;

        return null;
    }

    private sealed record CataloguePackage(
        string Id,
        string? Name,
        string? Title,
        JsonElement Metadata,
        List<CatalogueResource> Resources);

    private sealed record CatalogueResource(
        string Id,
        string? Name,
        string? Url,
        long? ExpectedSize,
        JsonElement Metadata);

    private sealed record PackageManifest(
        string Id,
        string? Name,
        string? Title,
        JsonElement OriginalMetadata,
        List<ResourceReference> Resources);

    private sealed record ResourceReference(
        string Id,
        string? Name,
        string? Url,
        long? ExpectedSizeBytes,
        JsonElement OriginalMetadata,
        string? ArtifactSha256,
        string Status);

    private sealed record ArtifactManifest(
        string OriginalUrl,
        string? EffectiveUrl,
        string? Sha256,
        string? StoredPath,
        long? ActualSizeBytes,
        List<long> ExpectedSizesBytes,
        bool SizeMismatch,
        string? HttpStatus,
        string? ContentType,
        string Status,
        string? Error,
        DateTimeOffset RetrievedAt);

    private sealed record AcquisitionManifest(
        string Version,
        DateTimeOffset CreatedAt,
        string CataloguePath,
        int PackageCount,
        int ResourceRecordCount,
        int UniqueUrlCount,
        int InvalidUrlCount,
        List<PackageManifest> Packages,
        List<ArtifactManifest> Artifacts);
}

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure;

namespace RaceDay.Api.Services;

public sealed class AzureBlobService : IBlobService
{
    private readonly BlobServiceClient? _client;
    private readonly string _containerName;

    public AzureBlobService(IConfiguration configuration, BlobServiceClient? client = null)
    {
        var connectionString = configuration["RaceDayStorage:ConnectionString"];
        _containerName = configuration["RaceDayStorage:Container"] ?? "raceday-images";
        _client = client ?? (string.IsNullOrWhiteSpace(connectionString)
            ? null
            : new BlobServiceClient(connectionString));
    }

    public bool IsConfigured => _client is not null;

    public async Task<string> UploadImageAsync(Stream content, string contentType, string fileName,
        CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var container = _client.GetBlobContainerClient(_containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken);
        var blob = container.GetBlobClient($"{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}");
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, cancellationToken);
        return blob.Uri.ToString();
    }

    public async Task<BlobImage?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        if (_client is null)
        {
            throw new InvalidOperationException("Azure Blob Storage is not configured.");
        }

        var container = _client.GetBlobContainerClient(_containerName);
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Host, container.Uri.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The stored image URL is invalid.");
        }

        var containerPath = container.Uri.AbsolutePath.TrimEnd('/');
        var blobPathPrefix = containerPath + "/";
        if (!uri.AbsolutePath.StartsWith(blobPathPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The stored image URL is outside the configured container.");
        }

        var escapedBlobName = uri.AbsolutePath[blobPathPrefix.Length..];
        var blobName = string.Join('/', escapedBlobName.Split('/').Select(Uri.UnescapeDataString));
        try
        {
            var response = await container
                .GetBlobClient(blobName)
                .DownloadStreamingAsync(cancellationToken: cancellationToken);
            return new BlobImage(response.Value.Content, response.Value.Details.ContentType ?? "application/octet-stream");
        }
        catch (RequestFailedException exception) when (exception.Status == 404)
        {
            return null;
        }
    }
}

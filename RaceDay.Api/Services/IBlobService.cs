namespace RaceDay.Api.Services;

public interface IBlobService
{
    bool IsConfigured { get; }
    Task<string> UploadImageAsync(Stream content, string contentType, string fileName, CancellationToken cancellationToken = default);
    Task<BlobImage?> DownloadImageAsync(string imageUrl, CancellationToken cancellationToken = default);
}

public sealed record BlobImage(Stream Content, string ContentType);

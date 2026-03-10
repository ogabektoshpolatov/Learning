using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using MinioWebApi.Settings;
using Microsoft.Extensions.Options;

namespace MinioWebApi.Services;

/// <summary>
/// Concrete implementation of IMinioService.
/// This class talks directly to the MinIO server via the MinIO .NET SDK.
/// Registered in DI as Scoped (one instance per HTTP request).
/// </summary>
public class MinioService : IMinioService
{
    private readonly IMinioClient _minioClient;
    private readonly string _bucketName;
    private readonly ILogger<MinioService> _logger;

    // ─── Allowed file types ───────────────────────────────────────────────────
    // We only accept these MIME types for security reasons
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "application/pdf",
        "text/plain",
        "application/zip"
    };

    // ─── Constructor (Dependency Injection) ───────────────────────────────────
    // ASP.NET Core automatically passes IMinioClient and IOptions<MinioSettings>
    // because we registered them in Program.cs
    public MinioService(IMinioClient minioClient, IOptions<MinioSettings> settings, ILogger<MinioService> logger)
    {
        _minioClient = minioClient;
        _bucketName = settings.Value.BucketName;
        _logger = logger;
    }

    // ─── UPLOAD ───────────────────────────────────────────────────────────────
    public async Task<string> UploadFileAsync(IFormFile file, CancellationToken ct = default)
    {
        // 1. Validate file type
        if (!AllowedContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException($"File type '{file.ContentType}' is not allowed.");

        // 2. Validate file size (max 50 MB)
        const long maxBytes = 50 * 1024 * 1024;
        if (file.Length > maxBytes)
            throw new InvalidOperationException("File size exceeds the 50 MB limit.");

        // 3. Generate a unique object name to avoid overwriting files with same name
        //    Example: "2024-01-15_143022_photo.png"
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
        var objectName = $"{timestamp}_{file.FileName}";

        // 4. Make sure the bucket exists (creates it if it doesn't)
        await EnsureBucketExistsAsync(ct);

        // 5. Open the file stream and upload to MinIO
        await using var stream = file.OpenReadStream();

        var putArgs = new PutObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)               // name it will have in MinIO
            .WithStreamData(stream)               // the file content
            .WithObjectSize(file.Length)          // size in bytes
            .WithContentType(file.ContentType);   // MIME type (pdf, png, etc.)

        await _minioClient.PutObjectAsync(putArgs, ct);

        _logger.LogInformation("Uploaded file: {ObjectName} ({Size} bytes)", objectName, file.Length);

        return objectName; // return the name so the caller can reference it later
    }

    // ─── DOWNLOAD ─────────────────────────────────────────────────────────────
    public async Task<(Stream stream, string contentType)> DownloadFileAsync(string objectName, CancellationToken ct = default)
    {
        // We need to get the file's content type first (stored as metadata in MinIO)
        string contentType = "application/octet-stream"; // default fallback

        var statArgs = new StatObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName);

        // StatObject = get metadata WITHOUT downloading the file
        var stat = await _minioClient.StatObjectAsync(statArgs, ct);
        contentType = stat.ContentType ?? contentType;

        // Now download the actual file into a MemoryStream
        var memoryStream = new MemoryStream();

        var getArgs = new GetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithCallbackStream(async (stream, token) =>
            {
                // MinIO gives us the stream via callback — we copy it into memory
                await stream.CopyToAsync(memoryStream, token);
            });

        await _minioClient.GetObjectAsync(getArgs, ct);

        memoryStream.Position = 0; // Reset position so the caller can read from start

        _logger.LogInformation("Downloaded file: {ObjectName}", objectName);

        return (memoryStream, contentType);
    }

    // ─── LIST FILES ───────────────────────────────────────────────────────────
    public async Task<List<FileInfoDto>> ListFilesAsync(CancellationToken ct = default)
    {
        await EnsureBucketExistsAsync(ct);

        var files = new List<FileInfoDto>();

        var listArgs = new ListObjectsArgs()
            .WithBucket(_bucketName)
            .WithRecursive(true);

        // ✅ IAsyncEnumerable — await foreach bilan ishlatiladi
        await foreach (var item in _minioClient.ListObjectsEnumAsync(listArgs, ct))
        {
            files.Add(new FileInfoDto(
                Name:         item.Key,
                SizeBytes:    (long)item.Size,
                ContentType:  item.ContentType,
                LastModified: item.LastModifiedDateTime ?? DateTime.MinValue
            ));
        }

        _logger.LogInformation("Listed {Count} files in bucket '{Bucket}'", files.Count, _bucketName);

        return files;
    }

    // ─── DELETE ───────────────────────────────────────────────────────────────
    public async Task DeleteFileAsync(string objectName, CancellationToken ct = default)
    {
        var removeArgs = new RemoveObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName);

        await _minioClient.RemoveObjectAsync(removeArgs, ct);

        _logger.LogInformation("Deleted file: {ObjectName}", objectName);
    }

    // ─── PRESIGNED URL ────────────────────────────────────────────────────────
    // A presigned URL is a temporary public link to a private file.
    // You share it with someone and it expires after N minutes.
    public async Task<string> GetPresignedUrlAsync(string objectName, int expiryMinutes = 60, CancellationToken ct = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_bucketName)
            .WithObject(objectName)
            .WithExpiry(expiryMinutes * 60); // MinIO expects seconds

        var url = await _minioClient.PresignedGetObjectAsync(args);

        return url;
    }

    // ─── PRIVATE HELPER ───────────────────────────────────────────────────────
    // Called before upload/list to make sure our bucket exists.
    // If it doesn't exist yet, we create it automatically.
    private async Task EnsureBucketExistsAsync(CancellationToken ct)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(_bucketName);
        bool exists = await _minioClient.BucketExistsAsync(existsArgs, ct);

        if (!exists)
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_bucketName);
            await _minioClient.MakeBucketAsync(makeArgs, ct);
            _logger.LogInformation("Created bucket: {BucketName}", _bucketName);
        }
    }
}
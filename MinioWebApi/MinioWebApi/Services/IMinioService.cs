namespace MinioWebApi.Services;

/// <summary>
/// Interface that defines what file operations our service can do.
/// Using an interface = easy to swap MinIO for another storage later (e.g. Azure Blob).
/// Controllers depend on this interface, not the concrete class → clean architecture.
/// </summary>
public interface IMinioService
{
    /// <summary>Upload a file and return its saved object name.</summary>
    Task<string> UploadFileAsync(IFormFile file, CancellationToken ct = default);

    /// <summary>Download a file and return its stream + content type.</summary>
    Task<(Stream stream, string contentType)> DownloadFileAsync(string objectName, CancellationToken ct = default);

    /// <summary>List all files in the bucket.</summary>
    Task<List<FileInfoDto>> ListFilesAsync(CancellationToken ct = default);

    /// <summary>Delete a file by name.</summary>
    Task DeleteFileAsync(string objectName, CancellationToken ct = default);

    /// <summary>Generate a temporary public URL to access a file (valid for N minutes).</summary>
    Task<string> GetPresignedUrlAsync(string objectName, int expiryMinutes = 60, CancellationToken ct = default);
}

/// <summary>
/// Simple DTO (Data Transfer Object) returned when listing files.
/// DTO = a plain object we send back to the client — no internal MinIO types exposed.
/// </summary>
public record FileInfoDto(
    string Name,         // file name / object key
    long SizeBytes,    // file size
    string ContentType,  // e.g. "image/png", "application/pdf"
    DateTime LastModified
);
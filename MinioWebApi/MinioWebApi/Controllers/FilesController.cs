using Microsoft.AspNetCore.Mvc;
using MinioWebApi.Services;

namespace MinioWebApi.Controllers;

/// <summary>
/// REST API Controller for file operations.
/// All routes start with: /api/files
///
/// [ApiController]  → enables automatic model validation, 400 responses, etc.
/// [Route]          → sets the base URL for all actions in this controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IMinioService _minioService;

    // ASP.NET Core injects IMinioService automatically (registered in Program.cs)
    public FilesController(IMinioService minioService)
    {
        _minioService = minioService;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST /api/files/upload
    // Upload one file (pdf, png, jpg, etc.)
    // Content-Type: multipart/form-data
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost("upload")]
    [RequestSizeLimit(52_428_800)] // 50 MB max request size
    public async Task<IActionResult> Upload(
        IFormFile file,  // IFormFile = the uploaded file from the form
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided or file is empty." });

        try
        {
            var objectName = await _minioService.UploadFileAsync(file, ct);

            // Return 201 Created with info about the saved file
            return CreatedAtAction(nameof(Download), new { objectName }, new
            {
                message = "File uploaded successfully.",
                objectName = objectName,        // use this name to download/delete
                size = file.Length,
                contentType = file.ContentType
            });
        }
        catch (InvalidOperationException ex)
        {
            // Our own validation errors (wrong type, too big)
            return BadRequest(new { error = ex.Message });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/files/download/{objectName}
    // Download a file by its object name
    // Example: GET /api/files/download/2024-01-15_143022_photo.png
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("download/{objectName}")]
    public async Task<IActionResult> Download(string objectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return BadRequest(new { error = "Object name is required." });

        try
        {
            var (stream, contentType) = await _minioService.DownloadFileAsync(objectName, ct);

            // File() returns the stream as a downloadable response
            // The third param = the filename shown when user saves the file
            return File(stream, contentType, objectName);
        }
        catch (Exception ex) when (ex.Message.Contains("NoSuchKey") || ex.Message.Contains("not found"))
        {
            return NotFound(new { error = $"File '{objectName}' not found." });
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/files
    // List all files in the bucket
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var files = await _minioService.ListFilesAsync(ct);

        return Ok(new
        {
            totalFiles = files.Count,
            files = files
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DELETE /api/files/{objectName}
    // Delete a file by its object name
    // ─────────────────────────────────────────────────────────────────────────
    [HttpDelete("{objectName}")]
    public async Task<IActionResult> Delete(string objectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return BadRequest(new { error = "Object name is required." });

        await _minioService.DeleteFileAsync(objectName, ct);

        return Ok(new { message = $"File '{objectName}' deleted successfully." });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET /api/files/url/{objectName}?expiryMinutes=30
    // Get a temporary public URL for a file (default: 60 minutes)
    // Share this URL with someone to let them download the file
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("url/{objectName}")]
    public async Task<IActionResult> GetPresignedUrl(
        string objectName,
        [FromQuery] int expiryMinutes = 60,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return BadRequest(new { error = "Object name is required." });

        var url = await _minioService.GetPresignedUrlAsync(objectName, expiryMinutes, ct);

        return Ok(new
        {
            url = url,
            expiresIn = $"{expiryMinutes} minutes",
            objectName = objectName
        });
    }
}
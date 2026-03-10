namespace MinioWebApi.Settings;

/// <summary>
/// Strongly-typed model that maps to the "MinIO" section in appsettings.json.
/// Instead of reading raw strings with IConfiguration["MinIO:Endpoint"],
/// we bind the whole section to this class — cleaner and type-safe.
/// </summary>
public class MinioSettings
{
    public string Endpoint { get; set; } = string.Empty;  // e.g. "localhost:9000"
    public string AccessKey { get; set; } = string.Empty;  // username
    public string SecretKey { get; set; } = string.Empty;  // password
    public string BucketName { get; set; } = string.Empty;  // default bucket
    public bool UseSSL { get; set; } = false;         // HTTP vs HTTPS
}
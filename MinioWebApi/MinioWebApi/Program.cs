using Minio;
using MinioWebApi.Services;
using MinioWebApi.Settings;

// ─────────────────────────────────────────────────────────────────────────────
// Program.cs — the entry point of the app.
// Two responsibilities:
//   1. Register services (Dependency Injection setup)
//   2. Configure the HTTP pipeline (middleware)
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);

// ── 1. Read MinIO settings from appsettings.json ─────────────────────────────
// This binds the "MinIO" section to our MinioSettings class.
// Now anywhere we inject IOptions<MinioSettings>, we get these values.
builder.Services.Configure<MinioSettings>(
    builder.Configuration.GetSection("MinIO")
);

// ── 2. Register MinIO Client ──────────────────────────────────────────────────
// We create the MinioClient using the settings from appsettings.json
// Registered as Singleton → one shared instance for the whole app (MinIO client is thread-safe)
builder.Services.AddSingleton<IMinioClient>(sp =>
{
    var settings = builder.Configuration.GetSection("MinIO").Get<MinioSettings>()!;

    var client = new MinioClient()
        .WithEndpoint(settings.Endpoint)
        .WithCredentials(settings.AccessKey, settings.SecretKey);

    // Only add SSL if UseSSL = true in appsettings (for local dev it's false)
    if (settings.UseSSL)
        client = client.WithSSL();

    return client.Build();
});

// ── 3. Register our MinIO Service ─────────────────────────────────────────────
// Scoped = new instance per HTTP request
// IMinioService interface → MinioService implementation
// Controllers depend on IMinioService (not MinioService directly = good practice)
builder.Services.AddScoped<IMinioService, MinioService>();

// ── 4. Add Controllers ────────────────────────────────────────────────────────
builder.Services.AddControllers();

// ── 5. Add Swagger (API documentation & testing UI) ───────────────────────────
// After running, go to http://localhost:5000/swagger to test your endpoints
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "MinIO File API",
        Version = "v1",
        Description = "Upload, download, list, and delete files using MinIO object storage."
    });
});

// ─────────────────────────────────────────────────────────────────────────────
// Build the app
// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── 6. Swagger middleware (only in development) ────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MinIO File API v1");
        c.RoutePrefix = "swagger"; // Swagger at /swagger
    });
}

// ── 7. Global error handling ──────────────────────────────────────────────────
// Returns clean JSON error responses instead of HTML error pages
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "An unexpected error occurred. Please try again."
        });
    });
});

// ── 8. Map controllers ─────────────────────────────────────────────────────────
// This tells ASP.NET to route incoming HTTP requests to our Controllers
app.MapControllers();

// ── 9. Run the app ─────────────────────────────────────────────────────────────
app.Run();
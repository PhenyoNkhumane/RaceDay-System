
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var defaultConnectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["RaceDaySql:ConnectionString"]
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection or RaceDaySql:ConnectionString is required.");

// Persist data-protection keys only when a path is configured (set in Docker),
// so local runs on Windows/macOS do not try to write to a root-level /app folder.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

// Controllers
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(defaultConnectionString));

// Identity
builder.Services.AddSingleton<IdentityNumberProtector>();

// Azure Blob Storage
var blobConnectionString =
    builder.Configuration["RaceDayStorage:ConnectionString"];

if (!string.IsNullOrWhiteSpace(blobConnectionString))
{
    builder.Services.AddSingleton(
        new BlobServiceClient(blobConnectionString));
}

// Blob service
builder.Services.AddSingleton<IBlobService, AzureBlobService>();

// Authentication
builder.Services.AddAuthentication("Session")
    .AddScheme<
        Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
        SessionAuthenticationHandler>(
            "Session", _ => { });

builder.Services.AddAuthorization();

// Server-side sessions
builder.Services.AddDistributedSqlServerCache(options =>
{
    options.ConnectionString = defaultConnectionString;
    options.SchemaName = "dbo";
    options.TableName = "SessionCache";
});

builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.SameAsRequest;

    options.IdleTimeout = TimeSpan.FromMinutes(60);
});

// Error handling
builder.Services.AddProblemDetails();

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.RejectionStatusCode =
        StatusCodes.Status429TooManyRequests;
});

// Swagger
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description =
            "Call POST /api/auth/login (or register) first. " +
            "The API starts a server-side session and the browser " +
            "sends the session cookie automatically, so Swagger " +
            "can then call protected endpoints."
    });
});

var app = builder.Build();

// Apply EF migrations on startup when enabled (Docker Compose turns this on)
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    migrationScope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>()
        .Database.Migrate();
}

// Middleware pipeline
if (app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseExceptionHandler();

app.UseRouting();

app.UseRateLimiter();

app.UseSession();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}

// File : /team-samsara/apps/api/src/TeamSamsara.Api/Program.cs
// Version : 1.1.0
// Latest commit: feature/api-host
// Author : Gerrah

// Purpose : Configures the API host, services, middleware pipeline, and modules.

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using TeamSamsara.Modules.PingPong;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Authorization;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Logging;
using TeamSamsara.Shared.Modules;
using TeamSamsara.Shared.Persistence;
using TeamSamsara.Shared.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSamsaraLogging();

const string FirebaseAuthScheme = "Firebase";

builder.Services
    .AddAuthentication(FirebaseAuthScheme)
    .AddScheme<
        AuthenticationSchemeOptions,
        FirebaseAuthenticationHandler>(
        FirebaseAuthScheme,
        null);

builder.Services.AddPermissionAuthorization();
builder.Services.AddFirestore(builder.Configuration);
builder.Services.AddFirebaseStorage(builder.Configuration);

builder.Services.AddValidatedOptions<CorsSettings>(
    builder.Configuration,
    "Cors");

var corsSettings =
    builder.Configuration
        .GetSection("Cors")
        .Get<CorsSettings>()
    ?? new CorsSettings();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy
            .WithOrigins(corsSettings.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddHealthChecks()
    .AddCheck<FirestoreHealthCheck>(
        "firestore",
        tags: new[] { "ready" });

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy =
        new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    // TODO: Configure trusted proxy ranges before production deployment.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Registers modules with the API host.
var modules = new List<IModule>
{
    new PingPongModule()
};

foreach (var module in modules)
{
    module.RegisterServices(
        builder.Services,
        builder.Configuration);
}

builder.Services.AddOpenApi();

var app = builder.Build();

if (FirebaseApp.DefaultInstance is null)
{
    if (app.Environment.IsDevelopment())
    {
        // Provides a credential for Firebase Auth emulator initialization.
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromAccessToken("owner")
        });
    }
    else
    {
        FirebaseApp.Create();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });

foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();

public partial class Program { }

// File : /team-samsara/apps/api/src/TeamSamsara.Api/Program.cs
// Version : 1.6.0
// Latest commit: feat/password-reset-endpoints
// Author : Gerrah
// Purpose : Configures the API host, services, middleware pipeline, and modules.

using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using TeamSamsara.Modules.Alert;
using TeamSamsara.Modules.Assets;
using TeamSamsara.Modules.Identity;
using TeamSamsara.Modules.PingPong;
using TeamSamsara.Shared.Authentication;
using TeamSamsara.Shared.Authorization;
using TeamSamsara.Shared.BackgroundTasks;
using TeamSamsara.Shared.Configuration;
using TeamSamsara.Shared.Email;
using TeamSamsara.Shared.Http;
using TeamSamsara.Shared.Logging;
using TeamSamsara.Shared.Modules;
using TeamSamsara.Shared.Persistence;
using TeamSamsara.Shared.Serialization;
using TeamSamsara.Shared.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSamsaraLogging();

const string FirebaseAuthScheme = "Firebase";

builder.Services
    .AddAuthentication(FirebaseAuthScheme)
    .AddScheme<AuthenticationSchemeOptions, FirebaseAuthenticationHandler>(FirebaseAuthScheme, null);

builder.Services.AddPermissionAuthorization();
builder.Services.AddFirestore(builder.Configuration);
builder.Services.AddAssetStorage(builder.Configuration);
builder.Services.AddEmail(builder.Configuration);
builder.Services.AddBackgroundTasks();

builder.Services.AddValidatedOptions<CorsSettings>(builder.Configuration, "Cors");
builder.Services.AddValidatedOptions<RateLimitSettings>(builder.Configuration, RateLimitSettings.SectionName);

var corsSettings = builder.Configuration.GetSection("Cors").Get<CorsSettings>() ?? new CorsSettings();

var rateLimitSettings = builder.Configuration
    .GetSection(RateLimitSettings.SectionName)
    .Get<RateLimitSettings>() ?? new RateLimitSettings();

builder.Services.AddSamsaraRateLimiting(rateLimitSettings);

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
    {
        policy.WithOrigins(corsSettings.AllowedOrigins).AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks().AddCheck<FirestoreHealthCheck>("firestore", tags: new[] { "ready" });

builder.Services.AddRequestTimeouts(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Http.Timeouts.RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(30)
    };
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});

// Only proxies on private networks (plus loopback, which is trusted by default) may set the
// forwarded headers: the platform's load balancer reaches the service over its private
// network. Trusting every sender would let any client forge its IP address, which GuardDog
// uses to recognize a member's devices.
var trustedProxyNetworks = new[]
{
    ("10.0.0.0", 8),
    ("172.16.0.0", 12),
    ("192.168.0.0", 16),
    ("fc00::", 7)
};

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Behind Render the client is the visitor Cloudflare saw. X-Forwarded-For ends with
    // Cloudflare's edge and Render's own private hop (visitor, edge, internal hop), so
    // reading its last entry would return that internal hop instead of the visitor.
    // CF-Connecting-IP holds only the visitor's address and is set by Cloudflare itself.
    // On Render the immediate peer is loopback (the platform proxy runs beside the app),
    // which is trusted by default. The header is ignored when absent (local dev).
    options.ForwardedForHeaderName = "CF-Connecting-IP";

    foreach (var (prefix, prefixLength) in trustedProxyNetworks)
    {
        options.KnownNetworks.Add(
            new Microsoft.AspNetCore.HttpOverrides.IPNetwork(System.Net.IPAddress.Parse(prefix), prefixLength));
    }
});

// Applies the shared JSON convention (camelCase properties, string enums) to every
// Minimal API endpoint's request/response serialization.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = DefaultJsonOptions.Instance.PropertyNamingPolicy;

    foreach (var converter in DefaultJsonOptions.Instance.Converters)
    {
        options.SerializerOptions.Converters.Add(converter);
    }
});

var modules = new List<IModule>
{
    new PingPongModule(),
    new AssetsModule(),
    new AlertModule(),
    new IdentityModule()
};

foreach (var module in modules)
{
    module.RegisterServices(builder.Services, builder.Configuration);
}

builder.Services.AddOpenApi();

var app = builder.Build();

if (FirebaseApp.DefaultInstance is null)
{
    // The project id is required to verify ID tokens (the audience must match it).
    var firebaseProjectId = app.Configuration["Firestore:ProjectId"];

    if (app.Environment.IsDevelopment())
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromAccessToken("owner"),
            ProjectId = firebaseProjectId
        });
    }
    else
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredentialProvider.TryGetFromEnvironment(),
            ProjectId = firebaseProjectId
        });
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

// After forwarded headers (real client IP) and routing (endpoint policies), before auth work.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseRequestTimeouts();

app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();

public partial class Program { }

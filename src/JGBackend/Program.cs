using System.Reflection;
using JGBackend.Configuration;
using Core.Features;
using Core.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Portfolio.Features;
using Portfolio.Persistence.Contexts;

var builder = WebApplication.CreateBuilder(args);

// User Secrets are a local development mechanism. Production and test hosts read
// configuration from environment variables; loading secrets there would shadow them.
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());
}

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddPortfolioFeatures(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddCorsOrigins(builder.Configuration, builder.Environment);
builder.Services.AddHealthChecks();
builder.Services.AddForwardedHeadersConfig();
builder.Services.AddRateLimitingConfig(builder.Configuration);

var app = builder.Build();

// Apply pending migrations. Gated so a deployment that applies them out of band (or a
// multi-replica setup) does not have every instance race to migrate. Production sets
// Database:AutoMigrate=false; the deploy workflow owns migrations there (see operations.md).
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
    dbContext.Database.Migrate();
}

// Configure the HTTP request pipeline.
// Must run before anything that reads the scheme or client IP (HTTPS redirect, rate limiting).
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("v1/swagger.json", "Core Backend v1");
    });
}

app.UseCors();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz").DisableRateLimiting().AllowAnonymous();
app.MapFeatures(RateLimitPolicies.Default);

// Portfolio endpoints declare only the segment below the version prefix. Core
// endpoints still carry their own /api/v1 group until the auth merge removes it.
// Only the composed host owns the middleware, so it supplies the policy names.
app.MapGroup("api/v1").MapPortfolioFeatures(
    RateLimitPolicies.NotionPassthrough,
    RateLimitPolicies.GithubPassthrough,
    RateLimitPolicies.Default,
    RateLimitPolicies.AdminWrite,
    CorsConfiguration.AdminCorsPolicy);

// The admin fallback would otherwise challenge unmatched routes, turning a 404
// into a 401. The explicit catch-all also matches dotted paths such as
// /swagger/v1/swagger.json when Swagger is disabled.
app.MapFallback("{*path}", () => Results.NotFound())
    .AllowAnonymous()
    .RequireRateLimiting(RateLimitPolicies.Default);

app.Run();

/// <summary>
/// Exposed so integration tests can boot the real host with
/// <c>WebApplicationFactory&lt;Program&gt;</c>. Top-level statements generate an
/// internal <c>Program</c> class; this only changes its accessibility.
/// </summary>
public partial class Program { }

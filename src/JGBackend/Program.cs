using System.Reflection;
using JGBackend.Configuration;
using Core.Features;
using Core.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;
using Portfolio.Features;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly());

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
builder.Services.AddRateLimitingConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Must run before anything that reads the scheme or client IP (HTTPS redirect, rate limiting).
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("v1/swagger.json", "Generic API v1");
    });
}

app.UseCors(CorsConfiguration.SpaCorsPolicy);
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapFeatures();

// Portfolio endpoints declare only the segment below the version prefix. Core
// endpoints still carry their own /api/v1 group until the auth merge removes it.
// Only the composed host runs a rate limiter, so it supplies the passthrough policies.
app.MapGroup("api/v1").MapPortfolioFeatures(
    RateLimitPolicies.NotionPassthrough,
    RateLimitPolicies.GithubPassthrough);

app.Run();

/// <summary>
/// Exposed so integration tests can boot the real host with
/// <c>WebApplicationFactory&lt;Program&gt;</c>. Top-level statements generate an
/// internal <c>Program</c> class; this only changes its accessibility.
/// </summary>
public partial class Program { }

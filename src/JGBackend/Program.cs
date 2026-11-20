using JGBackend.Configuration;
using Core.Features;
using Core.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddCorsOrigins();
builder.Services.AddHealthChecks();
builder.Services.AddForwardedHeadersConfig();
builder.Services.AddRateLimitingConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
// Must run before anything that reads the scheme or client IP (HTTPS redirect, rate limiting).
app.UseForwardedHeaders();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("v1/swagger.json", "Generic API v1");
    });
}

app.UseCors("AllowSPA");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/healthz");
app.MapFeatures();

app.Run();

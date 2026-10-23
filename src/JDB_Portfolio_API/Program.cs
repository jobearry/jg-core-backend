using JDB_Portfolio_API;
using JDB_Portfolio_API.Endpoints;
using JDB_Portfolio_API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// load configurations
builder.Configuration.AddUserSecrets<Program>();

// Add services to the container, one method per layer.
builder.Services.AddPresentation(builder.Configuration, builder.Environment);
builder.Services.AddApplication();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// Apply pending migrations. Gated so that a deployment where migrations are applied
// out of band (or a multi-replica setup) does not have every instance race to migrate.
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
    dbContext.Database.Migrate();
}

// First in the pipeline, so it sees everything the rest of the pipeline throws.
app.UseExceptionHandler();
app.UseCors(ConfigurationSettings.SpaCorsPolicy);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(ui =>
    {
        ui.SwaggerEndpoint("v1/swagger.json", "JDB Portfolio API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// One prefix, declared once. Endpoint files declare only the segment below it, so a
// future /api/v2 is one edit here instead of a hunt through seven files.
//
// The portfolio read endpoints are deliberately anonymous: they feed a public CV site.
// Only /auth/me requires a token. Adding .RequireAuthorization() to this group is a
// breaking change for the frontend, not a hardening tweak — see docs/api-reference.md.
var api = app.MapGroup("api/v1");

//endpoints
api.MapAuthEndpoints();
api.MapNotionEndpoints();

//portfolio endpoints
api.MapPortfolioExperienceEndpoints();
api.MapPortfolioProjectEndpoints();
api.MapPortfolioRecommendationEndpoints();
api.MapPortfolioSkillEndpoints();
api.MapOverview();

app.Run();

/// <summary>
/// Exposed so that <c>WebApplicationFactory&lt;Program&gt;</c> in
/// <c>tests/JDB_Portfolio_API.Api.Tests</c> can boot the real application. Top-level
/// statements generate an internal <c>Program</c> class; this makes it public without
/// adding any code to the startup path.
/// </summary>
public partial class Program { }
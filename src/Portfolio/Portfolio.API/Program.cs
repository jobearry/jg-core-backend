using Portfolio;
using Portfolio.Endpoints;
using Portfolio.Features.Experience;
using Portfolio.Features.Project;
using Portfolio.Features.Skill;
using Portfolio.Features.Recommendation;
using Portfolio.Persistence.Contexts;
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
// Swagger is Development-only by default. Swagger:Enabled lets a deployment expose it
// temporarily (e.g. a CRUD smoke test in Azure) without a code change; keep it false in
// production otherwise.
if (app.Environment.IsDevelopment() || app.Configuration.GetValue("Swagger:Enabled", false))
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
// future /api/v2 is one edit here instead of a hunt through endpoint files.
//
// Portfolio, Notion, and current GitHub reads are anonymous and return public data.
// Optional authenticated access to private GitHub metadata is not implemented yet.
var api = app.MapGroup("api/v1");

//endpoints
api.MapAuthEndpoints();
api.MapNotionEndpoints();
api.MapGithubEndpoints();

//portfolio endpoints
api.MapExperienceEndpoints();
api.MapProjectEndpoints();
api.MapRecommendationEndpoints();
api.MapSkillEndpoints();
api.MapOverview();

app.Run();

/// <summary>
/// Exposed so that <c>WebApplicationFactory&lt;Program&gt;</c> in
/// <c>tests/Portfolio.Api.Tests</c> can boot the real application. Top-level
/// statements generate an internal <c>Program</c> class; this makes it public without
/// adding any code to the startup path.
/// </summary>
public partial class Program { }
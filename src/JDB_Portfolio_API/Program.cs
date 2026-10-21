using JDB_Portfolio_API;
using JDB_Portfolio_API.Endpoints;
using JDB_Portfolio_API.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// load configurations
builder.Configuration.AddUserSecrets<Program>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddExternalService(builder.Configuration);
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddMapper(builder.Configuration);
builder.Services.AddCorsOrigins();

var app = builder.Build();

// Apply pending migrations. Gated so that a deployment where migrations are applied
// out of band (or a multi-replica setup) does not have every instance race to migrate.
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
    dbContext.Database.Migrate();
}

app.UseCors("AllowSPA");
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

app.MapControllers();

//endpoints
app.MapAuthEndpoints();
app.MapNotionEndpoints();

//portfolio endpoints
app.MapPortfolioExperienceEndpoints();
app.MapPortfolioProjectEndpoints();
app.MapPortfolioRecommendationEndpoints();
app.MapPortfolioSkillEndpoints();
app.MapOverview();

app.Run();
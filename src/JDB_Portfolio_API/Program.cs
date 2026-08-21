using JDB_Portfolio_API;
using JDB_Portfolio_API.Endpoints;

var builder = WebApplication.CreateBuilder(args);

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

app.Run();
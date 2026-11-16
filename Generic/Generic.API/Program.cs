using Generic.API.Configuration;
using Generic.Features;
using Generic.Infra;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatures();
builder.Services.AddAuthentication(builder.Configuration);
builder.Services.AddCorsOrigins();
builder.Services.AddHealthChecks();
builder.Services.AddRateLimitingConfig();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

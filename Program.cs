using Microsoft.EntityFrameworkCore;
using PokeREST.Models;
using PokeREST.Services;
using System.Reflection;

// Load environment variables from .env file. Must run before the builder is created and before
// DB_CONNECTION_STRING is read below.
ConfigurationService.LoadEnvironmentVariables();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // false is the framework default, kept explicit: [ApiController] rejects invalid request bodies with a
        // 400 ProblemDetails before the action runs, so the controllers' own ModelState checks are only a fallback.
        options.SuppressModelStateInvalidFilter = false;
    });

// Configure Entity Framework. The connection string comes straight from the environment (not IConfiguration),
// so appsettings.json cannot supply it; startup fails fast when it is missing.
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                      ?? throw new InvalidOperationException("Database connection string not found. Please check your .env file.");

builder.Services.AddDbContext<PokemonContext>(options =>
    options.UseNpgsql(connectionString));

// Register services. Scoped to match the DbContext lifetime (one instance per request).
builder.Services.AddScoped<ITrainerService, TrainerService>();
builder.Services.AddScoped<IPokemonService, PokemonService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pokemon Management API",
        Version = "v1",
        Description = "A comprehensive API for managing Pokemon trainers and their Pokemon collections."
    });

    // Include XML comments (produced by GenerateDocumentationFile in PokeREST.csproj). Skipped silently if the file
    // is missing, in which case Swagger shows endpoints without descriptions.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS (Allow all for demonstration purposes)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline. There is no global exception-handling middleware; each controller action
// catches its own exceptions and returns a generic 500 ApiResponse.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pokemon Management API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

// No authentication scheme or [Authorize] attributes are configured, so this currently has no effect.
app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using PokeREST.Models;
using PokeREST.Services;
using System.Reflection;

// Load environment variables from .env file
ConfigurationService.LoadEnvironmentVariables();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false;
    });

// Configure Entity Framework
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
                      ?? throw new InvalidOperationException("Database connection string not found. Please check your .env file.");

builder.Services.AddDbContext<PokemonContext>(options =>
    options.UseNpgsql(connectionString));

// Register services
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

    // Include XML comments
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

// Configure the HTTP request pipeline.
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

app.UseAuthorization();

app.MapControllers();

app.Run();

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Comms.Data;
using Comms.Hubs;
using Comms.Services;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables
var connectionString = Environment.ExpandEnvironmentVariables(
    builder.Configuration.GetConnectionString("DefaultConnection") ?? 
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found."));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add gRPC services
builder.Services.AddGrpc();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<CommunicationsDbContext>(options =>
    options.UseNpgsql(connectionString));

// SignalR removed - using NestJS WebSocket Gateway instead

// Add RabbitMQ services
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddHostedService<RabbitMQBackgroundService>();

// Add CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
var expandedOrigins = allowedOrigins.Select(Environment.ExpandEnvironmentVariables).ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(expandedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var secretKey = Environment.ExpandEnvironmentVariables(jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not found"));
var issuer = Environment.ExpandEnvironmentVariables(jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found"));
var audience = Environment.ExpandEnvironmentVariables(jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not found"));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };

        // Configure JWT for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/chathub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Add HTTP clients for microservice communication
builder.Services.AddHttpClient("ProfileService", client =>
{
    var baseUrl = Environment.ExpandEnvironmentVariables(
        builder.Configuration["Microservices:ProfileService"] ?? throw new InvalidOperationException("ProfileService URL not found"));
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("EventsService", client =>
{
    var baseUrl = Environment.ExpandEnvironmentVariables(
        builder.Configuration["Microservices:EventsService"] ?? throw new InvalidOperationException("EventsService URL not found"));
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("FilesService", client =>
{
    var baseUrl = Environment.ExpandEnvironmentVariables(
        builder.Configuration["Microservices:FilesService"] ?? throw new InvalidOperationException("FilesService URL not found"));
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use CORS before authentication
app.UseCors("AllowSpecificOrigins");

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<ChatHub>("/chathub");

// Health check endpoint
app.MapGet("/health", () => "Communications Service is running");

// Auto-migrate database on startup (optional - remove in production)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<CommunicationsDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

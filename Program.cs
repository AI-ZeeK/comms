using System.Text;
using System.Text.Json.Serialization;
using Comms.Data;
using Comms.Guards;
using Comms.Helpers;
using Comms.Hubs;
using Comms.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

// Load environment variables
var connectionString =
    Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

var redis_url = Environment.GetEnvironmentVariable("REDIS_URL") ?? "";

// // Add services to the container
// builder.Services.AddControllers()
builder
    .Services.AddControllers(options =>
    {
        options.Filters.Add<RejectExtraPropertiesFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Configure JSON serialization for snake_case compatibility with NestJS
        options.JsonSerializerOptions.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(new SnakeCaseNamingPolicy())
        );
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// builder.Services.AddControllers(options =>
// {
//     options.Conventions.Add(new RoutePrefixConvention("api/v1"));
// });

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Comms API", Version = "v1" });

    // Add JWT Bearer support
    c.AddSecurityDefinition(
        "Bearer",
        new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
            Scheme = "Bearer",
            BearerFormat = "JWT",
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Enter 'Bearer {your token}'",
        }
    );

    c.AddSecurityRequirement(
        new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
        {
            {
                new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Reference = new Microsoft.OpenApi.Models.OpenApiReference
                    {
                        Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                        Id = "Bearer",
                    },
                },
                new string[] { }
            },
        }
    );
});

// Console.WriteLine(JsonSerializer.Serialize(Environment.GetEnvironmentVariable("REDIS_URL")));
// builder.Services.AddSignalR();
builder
    .Services.AddSignalR()
    .AddStackExchangeRedis(
        redis_url,
        options =>
        {
            options.Configuration.ChannelPrefix = RedisChannel.Literal("CommsApp"); // optional
        }
    );
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redis_url));

builder.Services.AddSingleton<UserService>();
builder.Services.AddScoped<PushService>();

// Add gRPC services
builder.Services.AddScoped<IAdminGrpcService, AdminGrpcService>();
builder.Services.AddScoped<IProfileGrpcService, ProfileGrpcService>();

// Add guards
builder.Services.AddScoped<UserGuard>();
builder.Services.AddScoped<AdminGuard>();

// Add gRPC services
builder.Services.AddGrpc();

// Add Entity Framework with PostgreSQL
builder.Services.AddDbContext<CommunicationsDbContext>(options =>
    options.UseNpgsql(connectionString)
);

// SignalR removed - using NestJS WebSocket Gateway instead

// Add RabbitMQ services
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();
builder.Services.AddHostedService<RabbitMQBackgroundService>();

// Add CORS
var allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new string[0];
var expandedOrigins = allowedOrigins.Select(Environment.ExpandEnvironmentVariables).ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowSpecificOrigins",
        policy =>
        {
            policy
                .WithOrigins(expandedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials(); // Required for SignalR
        }
    );
});

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var userSecretKey =
    Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET")
    ?? throw new InvalidOperationException("JWT_ACCESS_SECRET not found");

var adminSecretKey =
    Environment.GetEnvironmentVariable("JWT_ADMIN_ACCESS_SECRET")
    ?? throw new InvalidOperationException("JWT_ADMIN_ACCESS_SECRET not found");
var issuer = Environment.ExpandEnvironmentVariables(
    jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer not found")
);
var audience = Environment.ExpandEnvironmentVariables(
    jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience not found")
);

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            // 🔑 Accept both admin & user secrets
            IssuerSigningKeys = new[]
            {
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(adminSecretKey)),
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(userSecretKey)),
            },
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
            },
        };
    });

builder.Services.AddAuthorization();

// Add HTTP clients for microservice communication
builder.Services.AddHttpClient(
    "ProfileService",
    client =>
    {
        var baseUrl = Environment.ExpandEnvironmentVariables(
            builder.Configuration["Microservices:ProfileService"]
                ?? throw new InvalidOperationException("ProfileService URL not found")
        );
        client.BaseAddress = new Uri(baseUrl);
    }
);

builder.Services.AddHttpClient(
    "EventsService",
    client =>
    {
        var baseUrl = Environment.ExpandEnvironmentVariables(
            builder.Configuration["Microservices:EventsService"]
                ?? throw new InvalidOperationException("EventsService URL not found")
        );
        client.BaseAddress = new Uri(baseUrl);
    }
);

builder.Services.AddHttpClient(
    "FilesService",
    client =>
    {
        var baseUrl = Environment.ExpandEnvironmentVariables(
            builder.Configuration["Microservices:FilesService"]
                ?? throw new InvalidOperationException("FilesService URL not found")
        );
        client.BaseAddress = new Uri(baseUrl);
    }
);

var app = builder.Build();

// var _logger = app.Logger;
// _logger.LogInformation("6688======", Environment.GetEnvironmentVariable("REDIS_URL"),  "&!*@#(!@&#*&!@(*!",userSecretKey);
// _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(jwtSettings["REDIS_URL"]));
// _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(Environment.GetEnvironmentVariable("JWT_ACCESS_SECRET") ));
// _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(jwtSettings["JWT_ACCESS_SECRET"]));
// _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(userSecretKey ));
// _logger.LogInformation(System.Text.Json.JsonSerializer.Serialize(jwtSettings));
// System.

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Comms API v1");
        c.RoutePrefix = "docs"; // You can access it at https://localhost:5001/docs
    });
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

app.MapGet(
    "/user",
    (UserService userService) =>
    {
        var user = userService.GetDummyUser();
        return Results.Ok(user);
    }
);

// Health check endpoint
// app.MapGet("/", () => "Communications Service is running");
app.MapGet(
    "/",
    () => new { message = "Communications Service is running on Watch", status = "ok" }
);

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

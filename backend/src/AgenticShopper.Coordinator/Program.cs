using System.Text;
using AgenticShopper.Agents.Receipt;
using AgenticShopper.Agents.Receipt.Interfaces;
using AgenticShopper.Agents.Receipt.Services;
using AgenticShopper.Coordinator.Hubs;
using AgenticShopper.Coordinator.Middleware;
using AgenticShopper.Core.Abstractions;
using AgenticShopper.Core.Interfaces;
using AgenticShopper.Core.Models;
using AgenticShopper.Core.Services;
using AgenticShopper.Data;
using AgenticShopper.Data.Repositories;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(new ConfigurationBuilder()
        .AddJsonFile("appsettings.json")
        .Build())
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/agentic-shopper-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting Agentic Shopper Coordinator");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog();

    // Add services to the container.
    // Configure PostgreSQL database
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add controllers
    builder.Services.AddControllers();

    // Register repositories
    builder.Services.AddScoped<IRepository<Receipt>, ReceiptRepository>();
    builder.Services.AddScoped<IRepository<Product>, ProductRepository>();
    builder.Services.AddScoped<ProductRepository>(); // For ProductController

    // Register services
    builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
    builder.Services.AddScoped<IOcrService, CompositeOcrService>();
    builder.Services.AddScoped<CsvExportService>();
    
    // TODO: Register LLM provider when needed
    // builder.Services.AddScoped<ILlmProvider>(sp => LlmProviderFactory.Create(...));

    // Register agents
    builder.Services.AddScoped<ReceiptAgent>();

    // Configure JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
    
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        
        // Configure JWT bearer for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                
                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // Configure SignalR
    builder.Services.AddSignalR();

    // Configure Redis distributed cache
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetValue<string>("Redis:ConnectionString");
        options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName");
    });

    // Configure rate limiting (DDoS protection, brute force prevention)
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.Configure<ClientRateLimitOptions>(builder.Configuration.GetSection("ClientRateLimiting"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    // Use error handling middleware
    app.UseMiddleware<ErrorHandlingMiddleware>();

    // Use Serilog request logging
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Agentic Shopper API V1");
            c.RoutePrefix = string.Empty; // Serve Swagger UI at app root
        });
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    // Use rate limiting middleware (MUST be before CORS, Authentication, Authorization)
    app.UseIpRateLimiting();
    app.UseClientRateLimiting();

    app.UseCors();

    // Use JWT authentication middleware
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapHub<ShoppingListHub>("/hubs/shopping-list");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

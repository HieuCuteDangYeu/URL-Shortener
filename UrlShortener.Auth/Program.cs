using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Auth.Application.Interfaces;
using UrlShortener.Auth.Application.Validators;
using UrlShortener.Auth.Infrastructure.Persistence;
using UrlShortener.Auth.Infrastructure.Security;
using UrlShortener.Auth.Presentation.Grpc;
using UrlShortener.Auth.Protos;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Shared.Protos;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger: use FullName for schema ids to avoid collisions
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Auth Service", Version = "v1" });
    c.CustomSchemaIds(t => t.FullName?.Replace("+", ".") ?? t.Name);
});

// Add gRPC (with JSON transcoding) and gRPC -> Swagger generator
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddGrpcSwagger();

// Database
builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// gRPC Client for UserService
var userServiceUrl = builder.Configuration["Services:UserService"] ?? "https://localhost:5002";
builder.Services.AddGrpcClient<UserService.UserServiceClient>(o =>
{
    o.Address = new Uri(userServiceUrl);
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    // For development: accept any SSL certificate
    var handler = new HttpClientHandler();
    if (builder.Environment.IsDevelopment())
    {
        handler.ServerCertificateCustomValidationCallback =
            HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
    }
    return handler;
});

// JWT Token Generator
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// FluentValidation
builder.Services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
builder.Services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
builder.Services.AddScoped<IValidator<RefreshTokenRequest>, RefreshTokenRequestValidator>();

// Application Services
builder.Services.AddScoped<IAuthService, UrlShortener.Auth.Application.Services.AuthService>();

// RabbitMQ
builder.Services.AddRabbitMqMessaging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Auth Service v1");
    });
}

// Only enable HTTPS redirection when HTTPS is configured
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrEmpty(configuredUrls) && configuredUrls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// Map gRPC service
app.MapGrpcService<AuthGrpcService>();

app.MapGet("/", () => "Auth Service - gRPC");

app.Run();

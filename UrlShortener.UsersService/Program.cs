using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.UserService.Application.Interfaces;
using UrlShortener.UserService.Infrastructure.Persistence;
using UrlShortener.UserService.Presentation.Grpc;
using UrlShortener.Shared.Protos;
using UrlShortener.UserService.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger: use FullName for schema ids to avoid collisions between generated proto types and app DTOs
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Users Service", Version = "v1" });
    c.CustomSchemaIds(t => t.FullName?.Replace("+", ".") ?? t.Name);
});

// Add gRPC (with JSON transcoding) and gRPC -> Swagger generator
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddGrpcSwagger();

// Database
builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// FluentValidation
builder.Services.AddScoped<IValidator<CreateUserRequest>, CreateUserRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateUserRequest>, UpdateUserRequestValidator>();
// Service DI
builder.Services.AddScoped<IUserService, UrlShortener.UserService.Application.Services.UserService>();

builder.Services.AddRabbitMqMessaging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Users Service");
    });
}

// Only enable HTTPS redirection when HTTPS is configured
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrEmpty(configuredUrls) && configuredUrls.Contains("https", StringComparison.OrdinalIgnoreCase))
{
    app.UseHttpsRedirection();
}

app.UseRouting();

// Map gRPC service implementation (JSON-transcoding routes are generated from proto annotations)
app.MapGrpcService<UsersGrpcService>();

app.MapGet("/", () => "User Service - gRPC");

app.Run();
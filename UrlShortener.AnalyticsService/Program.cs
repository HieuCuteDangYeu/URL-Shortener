using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.AnalyticsService.Application.Interfaces;
using UrlShortener.AnalyticsService.Application.Services;
using UrlShortener.AnalyticsService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Ensure Kestrel accepts both HTTP/1.1 and HTTP/2 on the HTTP port used for local/dev (allows Swagger UI + gRPC)
builder.WebHost.ConfigureKestrel(options =>
{
    // If you expose other ports via env (ASPNETCORE_URLS), adjust accordingly.
    options.ListenAnyIP(5005, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
    });
});

// Add gRPC + JSON transcoding and gRPC Swagger generator
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AnalyticsService", Version = "v1" });
});

// Database
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services / DI
builder.Services.AddScoped<IClickRepository, ClickRepository>();
builder.Services.AddControllers();

// FluentValidation: register validators here (create validators under Application.Validators)
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Message Broker (re-uses project Infrastructure)
builder.Services.AddRabbitMqMessaging();

// Optional: in-memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Analytics Service v1");
    });
}

// Only enable HTTPS redirection if an HTTPS URL is configured (prevents container HTTP->HTTPS redirect)
var configuredUrls = builder.Configuration["ASPNETCORE_URLS"] ??
                     Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
if (!string.IsNullOrEmpty(configuredUrls) &&
    configuredUrls.IndexOf("https", StringComparison.OrdinalIgnoreCase) >= 0)
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.MapControllers();               // JSON-transcoded REST endpoints from protos
app.MapGrpcService<AnalyticsGrpcService>();
app.MapGet("/", () => "Analytics Service - gRPC");

app.Run();
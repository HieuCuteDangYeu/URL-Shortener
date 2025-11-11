using Microsoft.EntityFrameworkCore;
using UrlShortener.AnalyticsService.Application.Interfaces;
using UrlShortener.AnalyticsService.Application.Services;
using UrlShortener.AnalyticsService.Infrastructure.Persistence;
using UrlShortener.AnalyticsService.Presentation.Grpc;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Infrastructure.Messaging.Events;

var builder = WebApplication.CreateBuilder(args);

// gRPC
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Analytics Service", Version = "v1" });
});

// DbContext
builder.Services.AddDbContext<AnalyticsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// DI
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Messaging
builder.Services.AddRabbitMqMessaging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Analytics Service v1");
    });
}

app.MapGrpcService<AnalyticsGrpcService>();
app.MapGet("/", () => "Analytics Service - gRPC");

// Subscribe to click events
var broker = app.Services.GetRequiredService<IMessageBroker>();
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();

broker.Subscribe<ShortLinkClickedEvent>("url-clicked", ev =>
{
    using var scope = scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AnalyticsDbContext>();
    db.Clicks.Add(new UrlShortener.AnalyticsService.Domain.Click
    {
        Id = Guid.NewGuid(),
        ShortCode = ev.ShortCode,
        CreatedAt = ev.CreatedAt,
        UserId = ev.UserId,
        UserAgent = ev.UserAgent,
        Browser = string.IsNullOrWhiteSpace(ev.Browser) ? "Unknown" : ev.Browser,
        Referer = ev.Referer
    });
    db.SaveChanges();
});

app.Run();
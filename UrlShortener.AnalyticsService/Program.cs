using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
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
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
});


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
    var browser = AnalyticsService.ParseBrowser(ev.UserAgent);
    Console.WriteLine(browser);
    db.Clicks.Add(new UrlShortener.AnalyticsService.Domain.Click
    {
        Id = Guid.NewGuid(),
        ShortCode = ev.ShortCode,
        CreatedAt = ev.CreatedAt,
        UserId = ev.UserId,
        UserAgent = ev.UserAgent,
        Browser = browser,
        Referer = ev.Referer
    });
    db.SaveChanges();

    var saved = db.Clicks.AsNoTracking().First(x => x.ShortCode == ev.ShortCode && x.CreatedAt == ev.CreatedAt);
});

app.Run();
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Infrastructure.Messaging;
using UrlShortener.Shared.Protos;
using UrlShortener.UrlShortenerService.Application.Interfaces;
using UrlShortener.UrlShortenerService.Application.Validators;
using UrlShortener.UrlShortenerService.Infrastructure.Persistance;
using UrlShortener.UrlShortenerService.Presentation.Grpc;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddGrpc().AddJsonTranscoding();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "UrlShortenerService", Version = "v1" });
});


// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Services
builder.Services.AddScoped<IValidator<CreateShortLinkRequest>, CreateShortLinkRequestValidator>();
builder.Services.AddScoped<IUrlShortenerService, UrlShortener.UrlShortenerService.Application.Services.UrlShortenerService>();
builder.Services.AddControllers();

// Message Broker
builder.Services.AddRabbitMqMessaging();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "UrlShortenerService v1");
    });
}

app.UseRouting();
app.UseHttpsRedirection();

app.MapControllers();
app.MapGrpcService<UrlShortenerGrpcService>();

app.MapGet("/", () => "URL Shortener Service - gRPC");

app.Run();

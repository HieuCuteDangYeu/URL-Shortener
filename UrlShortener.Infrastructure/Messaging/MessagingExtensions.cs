using Microsoft.Extensions.DependencyInjection;

namespace UrlShortener.Infrastructure.Messaging;

public static class MessagingExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(this IServiceCollection services)
    {
        services.AddSingleton<IMessageBroker, RabbitMqMessageBroker>();
        return services;
    }
}

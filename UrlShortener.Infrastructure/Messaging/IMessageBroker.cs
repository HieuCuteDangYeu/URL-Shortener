namespace UrlShortener.Infrastructure.Messaging;

public interface IMessageBroker
{
    void Dispose();
    void Publish<T>(string queueName, T message) where T : class;
    void Subscribe<T>(string queueName, Action<T> handler) where T : class;
}

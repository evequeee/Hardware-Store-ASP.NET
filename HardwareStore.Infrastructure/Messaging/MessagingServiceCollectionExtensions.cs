using HardwareStore.Domain.Events;
using HardwareStore.Infrastructure.Messaging.Consumers;
using HardwareStore.Infrastructure.Messaging.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace HardwareStore.Infrastructure.Messaging;

/// <summary>
/// Extension methods for registering messaging services
/// </summary>
public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Adds RabbitMQ messaging infrastructure services
    /// </summary>
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        // Register topology service
        services.AddSingleton<RabbitMQTopologyService>();
        
        // Register event publisher
        services.AddSingleton<IEventPublisher, RabbitMQEventPublisher>();
        
        // Register idempotency service
        services.AddSingleton<IIdempotencyService, InMemoryIdempotencyService>();
        
        // Register event handlers
        services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
        services.AddScoped<IEventHandler<ProductUpdatedEvent>, ProductUpdatedEventHandler>();
        services.AddScoped<IEventHandler<ProductDeletedEvent>, ProductDeletedEventHandler>();
        services.AddScoped<IEventHandler<ProductStockChangedEvent>, ProductStockChangedEventHandler>();
        services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedEventHandler>();
        services.AddScoped<IEventHandler<CustomerCreatedEvent>, CustomerCreatedEventHandler>();
        
        // Register event consumers as hosted services
        services.AddHostedService<ProductEventsConsumer>();
        services.AddHostedService<OrderEventsConsumer>();
        services.AddHostedService<CustomerEventsConsumer>();
        
        return services;
    }
    
    /// <summary>
    /// Adds Redis-based idempotency service (for production)
    /// </summary>
    public static IServiceCollection AddRedisIdempotency(this IServiceCollection services)
    {
        services.AddSingleton<IIdempotencyService, RedisIdempotencyService>();
        return services;
    }
}

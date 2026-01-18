using System.Text.Json;
using HardwareStore.Domain.Events;
using HardwareStore.Infrastructure.Messaging.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HardwareStore.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for product-related events
/// </summary>
public class ProductEventsConsumer : EventConsumerBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductEventsConsumer> _productLogger;
    private readonly IIdempotencyService _idempotencyService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public ProductEventsConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        IIdempotencyService idempotencyService,
        ILogger<ProductEventsConsumer> logger)
        : base(connection, logger, serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _idempotencyService = idempotencyService;
        _productLogger = logger;
    }

    protected override string QueueName => "products.events.queue";

    protected override async Task<bool> HandleMessageAsync(
        string eventType,
        string body,
        IReadOnlyBasicProperties properties,
        CancellationToken stoppingToken)
    {
        _productLogger.LogInformation(
            "Processing product event: {EventType}",
            eventType);

        try
        {
            // Route to appropriate handler based on event type
            var result = eventType switch
            {
                "ProductCreatedEvent" => await HandleProductCreated(body, stoppingToken),
                "ProductUpdatedEvent" => await HandleProductUpdated(body, stoppingToken),
                "ProductDeletedEvent" => await HandleProductDeleted(body, stoppingToken),
                "ProductStockChangedEvent" => await HandleProductStockChanged(body, stoppingToken),
                _ => HandleUnknownEvent(eventType)
            };

            return result;
        }
        catch (JsonException ex)
        {
            _productLogger.LogError(ex, "Failed to deserialize message for event type: {EventType}", eventType);
            return false; // Will trigger retry or DLX
        }
    }

    protected override async Task<bool> IsAlreadyProcessedAsync(string messageId, CancellationToken stoppingToken)
    {
        if (!Guid.TryParse(messageId, out var eventId))
        {
            return false;
        }
        
        return await _idempotencyService.IsProcessedAsync(eventId, stoppingToken);
    }

    protected override async Task MarkAsProcessedAsync(string messageId, CancellationToken stoppingToken)
    {
        if (Guid.TryParse(messageId, out var eventId))
        {
            await _idempotencyService.MarkAsProcessedAsync(eventId, "ProductEvent", stoppingToken);
        }
    }

    private async Task<bool> HandleProductCreated(string messageJson, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ProductCreatedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _productLogger.LogWarning("Failed to deserialize ProductCreatedEvent");
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<ProductCreatedEvent>>();
        return await handler.HandleAsync(@event, cancellationToken);
    }

    private async Task<bool> HandleProductUpdated(string messageJson, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ProductUpdatedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _productLogger.LogWarning("Failed to deserialize ProductUpdatedEvent");
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<ProductUpdatedEvent>>();
        return await handler.HandleAsync(@event, cancellationToken);
    }

    private async Task<bool> HandleProductDeleted(string messageJson, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ProductDeletedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _productLogger.LogWarning("Failed to deserialize ProductDeletedEvent");
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<ProductDeletedEvent>>();
        return await handler.HandleAsync(@event, cancellationToken);
    }

    private async Task<bool> HandleProductStockChanged(string messageJson, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<ProductStockChangedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _productLogger.LogWarning("Failed to deserialize ProductStockChangedEvent");
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<ProductStockChangedEvent>>();
        return await handler.HandleAsync(@event, cancellationToken);
    }

    private bool HandleUnknownEvent(string eventType)
    {
        _productLogger.LogWarning("Unknown event type: {EventType}, skipping message", eventType);
        return true; // Acknowledge to avoid redelivery of unknown events
    }
}

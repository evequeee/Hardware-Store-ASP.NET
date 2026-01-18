using System.Text.Json;
using HardwareStore.Domain.Events;
using HardwareStore.Infrastructure.Messaging.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace HardwareStore.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for order-related events
/// </summary>
public class OrderEventsConsumer : EventConsumerBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderEventsConsumer> _orderLogger;
    private readonly IIdempotencyService _idempotencyService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public OrderEventsConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        IIdempotencyService idempotencyService,
        ILogger<OrderEventsConsumer> logger)
        : base(connection, logger, serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _idempotencyService = idempotencyService;
        _orderLogger = logger;
    }

    protected override string QueueName => "orders.events.queue";

    protected override async Task<bool> HandleMessageAsync(
        string eventType,
        string body,
        IReadOnlyBasicProperties properties,
        CancellationToken stoppingToken)
    {
        _orderLogger.LogInformation(
            "Processing order event: {EventType}",
            eventType);

        try
        {
            var result = eventType switch
            {
                "OrderPlacedEvent" => await HandleOrderPlaced(body, stoppingToken),
                "OrderPaymentProcessedEvent" => HandleOrderPaymentProcessed(body),
                "OrderShippedEvent" => HandleOrderShipped(body),
                _ => HandleUnknownEvent(eventType)
            };

            return result;
        }
        catch (JsonException ex)
        {
            _orderLogger.LogError(ex, "Failed to deserialize message for event type: {EventType}", eventType);
            return false;
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
            await _idempotencyService.MarkAsProcessedAsync(eventId, "OrderEvent", stoppingToken);
        }
    }

    private async Task<bool> HandleOrderPlaced(string messageJson, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<OrderPlacedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _orderLogger.LogWarning("Failed to deserialize OrderPlacedEvent");
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<OrderPlacedEvent>>();
        return await handler.HandleAsync(@event, cancellationToken);
    }

    private bool HandleOrderPaymentProcessed(string messageJson)
    {
        var @event = JsonSerializer.Deserialize<OrderPaymentProcessedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _orderLogger.LogWarning("Failed to deserialize OrderPaymentProcessedEvent");
            return false;
        }

        _orderLogger.LogInformation(
            "Order payment processed: OrderId={OrderId}, PaymentMethod={Method}, Success={Success}",
            @event.OrderId,
            @event.PaymentMethod,
            @event.IsSuccessful);

        return true;
    }

    private bool HandleOrderShipped(string messageJson)
    {
        var @event = JsonSerializer.Deserialize<OrderShippedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _orderLogger.LogWarning("Failed to deserialize OrderShippedEvent");
            return false;
        }

        _orderLogger.LogInformation(
            "Order shipped: OrderId={OrderId}, TrackingNumber={Tracking}, Carrier={Carrier}",
            @event.OrderId,
            @event.TrackingNumber,
            @event.Carrier);

        return true;
    }

    private bool HandleUnknownEvent(string eventType)
    {
        _orderLogger.LogWarning("Unknown event type: {EventType}, skipping message", eventType);
        return true;
    }
}

/// <summary>
/// Consumer for customer-related events
/// </summary>
public class CustomerEventsConsumer : EventConsumerBackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CustomerEventsConsumer> _customerLogger;
    private readonly IIdempotencyService _idempotencyService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public CustomerEventsConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        IIdempotencyService idempotencyService,
        ILogger<CustomerEventsConsumer> logger)
        : base(connection, logger, serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _idempotencyService = idempotencyService;
        _customerLogger = logger;
    }

    protected override string QueueName => "customers.events.queue";

    protected override async Task<bool> HandleMessageAsync(
        string eventType,
        string body,
        IReadOnlyBasicProperties properties,
        CancellationToken stoppingToken)
    {
        _customerLogger.LogInformation(
            "Processing customer event: {EventType}",
            eventType);

        try
        {
            var result = eventType switch
            {
                "CustomerCreatedEvent" => await HandleCustomerCreated(body, stoppingToken),
                _ => HandleUnknownEvent(eventType)
            };

            return result;
        }
        catch (JsonException ex)
        {
            _customerLogger.LogError(ex, "Failed to deserialize message for event type: {EventType}", eventType);
            return false;
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
            await _idempotencyService.MarkAsProcessedAsync(eventId, "CustomerEvent", stoppingToken);
        }
    }

    private async Task<bool> HandleCustomerCreated(string messageJson, CancellationToken cancellationToken)
    {
        var @event = JsonSerializer.Deserialize<CustomerCreatedEvent>(messageJson, JsonOptions);
        if (@event is null)
        {
            _customerLogger.LogWarning("Failed to deserialize CustomerCreatedEvent");
            return false;
        }

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IEventHandler<CustomerCreatedEvent>>();
        return await handler.HandleAsync(@event, cancellationToken);
    }

    private bool HandleUnknownEvent(string eventType)
    {
        _customerLogger.LogWarning("Unknown event type: {EventType}, skipping message", eventType);
        return true;
    }
}

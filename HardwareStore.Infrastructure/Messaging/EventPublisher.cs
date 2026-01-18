using HardwareStore.Domain.Events;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HardwareStore.Infrastructure.Messaging;

/// <summary>
/// Interface for publishing domain events to message broker
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent;
    
    Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent;
}

/// <summary>
/// RabbitMQ implementation of event publisher with distributed tracing support
/// Uses RabbitMQ.Client 7.x async API
/// </summary>
public class RabbitMQEventPublisher : IEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    
    // Exchange names for different event types
    private const string ProductsExchange = "hardwarestore.products.events";
    private const string OrdersExchange = "hardwarestore.orders.events";
    private const string CustomersExchange = "hardwarestore.customers.events";
    private const string DeadLetterExchange = "hardwarestore.deadletter.exchange";

    public RabbitMQEventPublisher(
        IConnection connection,
        ILogger<RabbitMQEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent
    {
        var routingKey = @event.EventType;
        await PublishAsync(@event, routingKey, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(TEvent @event, string routingKey, CancellationToken cancellationToken = default) 
        where TEvent : IDomainEvent
    {
        var exchange = GetExchangeForEvent(@event);
        
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        
        try
        {
            // Ensure exchange exists
            await EnsureExchangeExistsAsync(channel, exchange, cancellationToken);
            
            // Serialize event to JSON
            var messageBody = JsonSerializer.SerializeToUtf8Bytes(@event, _jsonOptions);
            
            // Create basic properties with headers for tracing
            var properties = new BasicProperties
            {
                ContentType = "application/json",
                ContentEncoding = "utf-8",
                Persistent = true,
                MessageId = @event.EventId.ToString(),
                CorrelationId = @event.CorrelationId,
                Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
                Type = @event.EventType,
                Headers = new Dictionary<string, object?>()
            };
            
            // Add trace context headers for distributed tracing
            AddTraceContextHeaders(properties, @event);
            
            // Publish message
            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: messageBody,
                cancellationToken: cancellationToken);
            
            _logger.LogInformation(
                "Published event {EventType} with ID {EventId} to exchange {Exchange} with routing key {RoutingKey}. CorrelationId: {CorrelationId}",
                @event.EventType,
                @event.EventId,
                exchange,
                routingKey,
                @event.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to publish event {EventType} with ID {EventId} to exchange {Exchange}",
                @event.EventType,
                @event.EventId,
                exchange);
            throw;
        }
    }

    private static string GetExchangeForEvent(IDomainEvent @event)
    {
        return @event.EventType switch
        {
            var t when t.StartsWith("Product") => ProductsExchange,
            var t when t.StartsWith("Order") => OrdersExchange,
            var t when t.StartsWith("Customer") => CustomersExchange,
            _ => ProductsExchange
        };
    }

    private async Task EnsureExchangeExistsAsync(IChannel channel, string exchange, CancellationToken cancellationToken)
    {
        // Declare exchange (topic type for flexible routing)
        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
        
        // Declare dead letter exchange
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);
    }

    private void AddTraceContextHeaders(BasicProperties properties, IDomainEvent @event)
    {
        var activity = Activity.Current;
        
        if (activity != null)
        {
            // Add W3C trace context
            properties.Headers!["traceparent"] = activity.Id ?? string.Empty;
            
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                properties.Headers["tracestate"] = activity.TraceStateString;
            }
            
            properties.Headers["trace-id"] = activity.TraceId.ToString();
            properties.Headers["span-id"] = activity.SpanId.ToString();
        }
        
        // Add event metadata
        properties.Headers!["event-type"] = @event.EventType;
        properties.Headers["event-version"] = @event.Version;
        properties.Headers["correlation-id"] = @event.CorrelationId;
        properties.Headers["event-id"] = @event.EventId.ToString();
        properties.Headers["timestamp"] = @event.Timestamp.ToString("O");
        properties.Headers["retry-count"] = 0;
    }
}

/// <summary>
/// RabbitMQ topology setup service for creating exchanges and queues
/// Uses RabbitMQ.Client 7.x async API
/// </summary>
public class RabbitMQTopologyService
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMQTopologyService> _logger;

    // Exchange names
    private const string ProductsExchange = "hardwarestore.products.events";
    private const string OrdersExchange = "hardwarestore.orders.events";
    private const string CustomersExchange = "hardwarestore.customers.events";
    private const string DeadLetterExchange = "hardwarestore.deadletter.exchange";

    // Queue names
    private const string ProductEventsQueue = "products.events.queue";
    private const string OrderEventsQueue = "orders.events.queue";
    private const string CustomerEventsQueue = "customers.events.queue";
    private const string DeadLetterQueue = "hardwarestore.deadletter.queue";

    public RabbitMQTopologyService(
        IConnection connection,
        ILogger<RabbitMQTopologyService> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task SetupTopologyAsync(CancellationToken cancellationToken = default)
    {
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        _logger.LogInformation("Setting up RabbitMQ topology...");

        // 1. Declare Dead Letter Exchange and Queue
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: "deadletter",
            cancellationToken: cancellationToken);

        // 2. Declare Products Exchange and Queues
        await channel.ExchangeDeclareAsync(
            exchange: ProductsExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        var deadLetterArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", DeadLetterExchange },
            { "x-dead-letter-routing-key", "deadletter" }
        };

        await channel.QueueDeclareAsync(
            queue: ProductEventsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArgs,
            cancellationToken: cancellationToken);

        // Bind queue to all product events (using # for wildcard)
        await channel.QueueBindAsync(
            queue: ProductEventsQueue,
            exchange: ProductsExchange,
            routingKey: "products.#",
            cancellationToken: cancellationToken);

        // 3. Declare Orders Exchange and Queue
        await channel.ExchangeDeclareAsync(
            exchange: OrdersExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: OrderEventsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: OrderEventsQueue,
            exchange: OrdersExchange,
            routingKey: "orders.#",
            cancellationToken: cancellationToken);

        // 4. Declare Customers Exchange and Queue
        await channel.ExchangeDeclareAsync(
            exchange: CustomersExchange,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: CustomerEventsQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArgs,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: CustomerEventsQueue,
            exchange: CustomersExchange,
            routingKey: "customers.#",
            cancellationToken: cancellationToken);

        _logger.LogInformation("RabbitMQ topology setup completed successfully");
    }
}

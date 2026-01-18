using HardwareStore.Domain.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HardwareStore.Infrastructure.Messaging;

/// <summary>
/// Base class for event consumers implementing BackgroundService pattern
/// Uses RabbitMQ.Client 7.x async API
/// </summary>
public abstract class EventConsumerBackgroundService : BackgroundService
{
    protected readonly IConnection Connection;
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    protected IChannel? Channel;
    
    protected abstract string QueueName { get; }
    protected virtual ushort PrefetchCount => 10;
    protected const int MaxRetryAttempts = 3;

    protected EventConsumerBackgroundService(
        IConnection connection,
        ILogger logger,
        IServiceProvider serviceProvider)
    {
        Connection = connection;
        Logger = logger;
        ServiceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Starting event consumer for queue {QueueName}", QueueName);
        
        try
        {
            Channel = await Connection.CreateChannelAsync(cancellationToken: stoppingToken);
            
            // Set QoS for prefetch
            await Channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, cancellationToken: stoppingToken);
            
            var consumer = new AsyncEventingBasicConsumer(Channel);
            
            consumer.ReceivedAsync += async (sender, args) =>
            {
                await ProcessMessageAsync(args, stoppingToken);
            };
            
            // Start consuming with manual acknowledgment
            await Channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);
            
            Logger.LogInformation("Event consumer started for queue {QueueName}", QueueName);
            
            // Keep running until cancellation
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            Logger.LogInformation("Event consumer stopping for queue {QueueName}", QueueName);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in event consumer for queue {QueueName}", QueueName);
            throw;
        }
    }

    private async Task ProcessMessageAsync(BasicDeliverEventArgs args, CancellationToken stoppingToken)
    {
        var messageId = args.BasicProperties.MessageId ?? Guid.NewGuid().ToString();
        var eventType = args.BasicProperties.Type ?? "unknown";
        var correlationId = args.BasicProperties.CorrelationId ?? string.Empty;
        
        // Restore trace context from headers
        using var activity = RestoreTraceContext(args);
        
        try
        {
            Logger.LogInformation(
                "Processing message {MessageId} of type {EventType} from queue {QueueName}. CorrelationId: {CorrelationId}",
                messageId,
                eventType,
                QueueName,
                correlationId);
            
            var body = Encoding.UTF8.GetString(args.Body.ToArray());
            
            // Check for idempotency
            if (await IsAlreadyProcessedAsync(messageId, stoppingToken))
            {
                Logger.LogWarning(
                    "Duplicate message detected: {MessageId}. Skipping processing.",
                    messageId);
                
                await Channel!.BasicAckAsync(args.DeliveryTag, multiple: false, stoppingToken);
                return;
            }
            
            // Process the message
            var success = await HandleMessageAsync(eventType, body, args.BasicProperties, stoppingToken);
            
            if (success)
            {
                // Mark as processed for idempotency
                await MarkAsProcessedAsync(messageId, stoppingToken);
                
                // Acknowledge successful processing
                await Channel!.BasicAckAsync(args.DeliveryTag, multiple: false, stoppingToken);
                
                Logger.LogInformation(
                    "Successfully processed message {MessageId} of type {EventType}",
                    messageId,
                    eventType);
            }
            else
            {
                // Check retry count
                var retryCount = GetRetryCount(args.BasicProperties);
                
                if (retryCount < MaxRetryAttempts)
                {
                    Logger.LogWarning(
                        "Message {MessageId} processing failed. Retry {RetryCount}/{MaxRetries}. Requeuing...",
                        messageId,
                        retryCount + 1,
                        MaxRetryAttempts);
                    
                    // Negative acknowledge with requeue for retry
                    await Channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, stoppingToken);
                }
                else
                {
                    Logger.LogError(
                        "Message {MessageId} exceeded max retries ({MaxRetries}). Sending to dead letter queue.",
                        messageId,
                        MaxRetryAttempts);
                    
                    // Negative acknowledge without requeue - goes to DLX
                    await Channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, stoppingToken);
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex,
                "Exception processing message {MessageId} of type {EventType}",
                messageId,
                eventType);
            
            // Check retry count
            var retryCount = GetRetryCount(args.BasicProperties);
            
            if (retryCount < MaxRetryAttempts)
            {
                await Channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, stoppingToken);
            }
            else
            {
                await Channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false, stoppingToken);
            }
        }
    }

    protected abstract Task<bool> HandleMessageAsync(
        string eventType, 
        string body, 
        IReadOnlyBasicProperties properties,
        CancellationToken stoppingToken);

    protected virtual Task<bool> IsAlreadyProcessedAsync(string messageId, CancellationToken stoppingToken)
    {
        // Override in derived class to implement idempotency checking
        return Task.FromResult(false);
    }

    protected virtual Task MarkAsProcessedAsync(string messageId, CancellationToken stoppingToken)
    {
        // Override in derived class to implement idempotency marking
        return Task.CompletedTask;
    }

    private int GetRetryCount(IReadOnlyBasicProperties properties)
    {
        if (properties.Headers != null && 
            properties.Headers.TryGetValue("retry-count", out var retryCountObj) &&
            retryCountObj is int retryCount)
        {
            return retryCount;
        }
        return 0;
    }

    private Activity? RestoreTraceContext(BasicDeliverEventArgs args)
    {
        if (args.BasicProperties.Headers == null)
            return null;

        string? traceParent = null;
        string? traceState = null;

        if (args.BasicProperties.Headers.TryGetValue("traceparent", out var traceParentObj))
        {
            traceParent = traceParentObj switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string str => str,
                _ => null
            };
        }

        if (args.BasicProperties.Headers.TryGetValue("tracestate", out var traceStateObj))
        {
            traceState = traceStateObj switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string str => str,
                _ => null
            };
        }

        if (string.IsNullOrEmpty(traceParent))
            return null;

        var activitySource = new ActivitySource("HardwareStore.Messaging");
        var activity = activitySource.StartActivity(
            $"process {QueueName}",
            ActivityKind.Consumer,
            traceParent);

        if (activity != null && !string.IsNullOrEmpty(traceState))
        {
            activity.TraceStateString = traceState;
        }

        // Add event-specific tags
        var eventType = args.BasicProperties.Type ?? "unknown";
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", QueueName);
        activity?.SetTag("messaging.operation", "process");
        activity?.SetTag("event.type", eventType);
        activity?.SetTag("message.id", args.BasicProperties.MessageId);

        return activity;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping event consumer for queue {QueueName}", QueueName);
        
        if (Channel != null)
        {
            await Channel.CloseAsync(cancellationToken);
            await Channel.DisposeAsync();
        }
        
        await base.StopAsync(cancellationToken);
    }
}

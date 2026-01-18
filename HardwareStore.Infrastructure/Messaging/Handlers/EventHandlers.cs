using HardwareStore.Domain.Events;
using Microsoft.Extensions.Logging;

namespace HardwareStore.Infrastructure.Messaging.Handlers;

/// <summary>
/// Interface for event handlers
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task<bool> HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}

/// <summary>
/// Handler for ProductCreatedEvent
/// </summary>
public class ProductCreatedEventHandler : IEventHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling ProductCreatedEvent: ProductId={ProductId}, Name={Name}, Category={Category}, Price={Price}",
            @event.ProductId,
            @event.Name,
            @event.Category,
            @event.Price);

        // Business logic:
        // - Update read models / materialized views
        // - Update search indexes
        // - Send notifications
        // - Update analytics
        
        await Task.Delay(10, cancellationToken); // Simulate async work
        
        _logger.LogInformation(
            "Successfully handled ProductCreatedEvent for ProductId={ProductId}",
            @event.ProductId);
        
        return true;
    }
}

/// <summary>
/// Handler for ProductUpdatedEvent
/// </summary>
public class ProductUpdatedEventHandler : IEventHandler<ProductUpdatedEvent>
{
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(ILogger<ProductUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ProductUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling ProductUpdatedEvent: ProductId={ProductId}, Name={Name}, PriceChange={PreviousPrice}->{NewPrice}",
            @event.ProductId,
            @event.Name,
            @event.PreviousPrice,
            @event.Price);

        // Business logic:
        // - Update read models
        // - Invalidate cache entries
        // - Check for significant price changes -> notify customers
        // - Update inventory tracking
        
        var priceChanged = @event.Price != @event.PreviousPrice;
        if (priceChanged)
        {
            _logger.LogInformation(
                "Price changed for product {ProductId}: {PreviousPrice} -> {NewPrice}",
                @event.ProductId,
                @event.PreviousPrice,
                @event.Price);
        }

        var stockChanged = @event.StockQuantity != @event.PreviousStockQuantity;
        if (stockChanged)
        {
            _logger.LogInformation(
                "Stock changed for product {ProductId}: {PreviousStock} -> {NewStock}",
                @event.ProductId,
                @event.PreviousStockQuantity,
                @event.StockQuantity);
        }
        
        await Task.Delay(10, cancellationToken);
        
        return true;
    }
}

/// <summary>
/// Handler for ProductDeletedEvent
/// </summary>
public class ProductDeletedEventHandler : IEventHandler<ProductDeletedEvent>
{
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(ILogger<ProductDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ProductDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling ProductDeletedEvent: ProductId={ProductId}, Name={Name}, Reason={Reason}",
            @event.ProductId,
            @event.ProductName,
            @event.Reason);

        // Business logic:
        // - Remove from read models
        // - Remove from search indexes
        // - Archive product data
        // - Notify affected customers
        
        await Task.Delay(10, cancellationToken);
        
        return true;
    }
}

/// <summary>
/// Handler for ProductStockChangedEvent
/// </summary>
public class ProductStockChangedEventHandler : IEventHandler<ProductStockChangedEvent>
{
    private readonly ILogger<ProductStockChangedEventHandler> _logger;

    public ProductStockChangedEventHandler(ILogger<ProductStockChangedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(ProductStockChangedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling ProductStockChangedEvent: ProductId={ProductId}, Change={Change}, NewQuantity={NewQuantity}, IsLowStock={IsLowStock}",
            @event.ProductId,
            @event.QuantityChange,
            @event.NewQuantity,
            @event.IsLowStock);

        // Business logic:
        // - Update inventory dashboards
        // - Send low stock alerts
        // - Trigger reorder workflows
        
        if (@event.IsLowStock)
        {
            _logger.LogWarning(
                "LOW STOCK ALERT: Product {ProductId} ({ProductName}) has only {Quantity} items remaining",
                @event.ProductId,
                @event.ProductName,
                @event.NewQuantity);
        }
        
        await Task.Delay(10, cancellationToken);
        
        return true;
    }
}

/// <summary>
/// Handler for OrderPlacedEvent
/// </summary>
public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling OrderPlacedEvent: OrderId={OrderId}, CustomerId={CustomerId}, TotalAmount={TotalAmount}, ItemCount={ItemCount}",
            @event.OrderId,
            @event.CustomerId,
            @event.TotalAmount,
            @event.Items.Count);

        // Business logic:
        // - Reserve inventory
        // - Calculate shipping
        // - Send order confirmation email
        // - Update customer purchase history
        
        await Task.Delay(10, cancellationToken);
        
        return true;
    }
}

/// <summary>
/// Handler for CustomerCreatedEvent  
/// </summary>
public class CustomerCreatedEventHandler : IEventHandler<CustomerCreatedEvent>
{
    private readonly ILogger<CustomerCreatedEventHandler> _logger;

    public CustomerCreatedEventHandler(ILogger<CustomerCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HandleAsync(CustomerCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling CustomerCreatedEvent: CustomerId={CustomerId}, Email={Email}, Type={CustomerType}",
            @event.CustomerId,
            @event.Email,
            @event.CustomerType);

        // Business logic:
        // - Send welcome email
        // - Create customer profile in CRM
        // - Apply welcome discounts
        // - Add to marketing segments
        
        await Task.Delay(10, cancellationToken);
        
        return true;
    }
}

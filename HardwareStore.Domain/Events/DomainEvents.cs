namespace HardwareStore.Domain.Events;

/// <summary>
/// Base interface for all domain events
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }
    string CorrelationId { get; }
    DateTime Timestamp { get; }
    string EventType { get; }
    int Version { get; }
}

/// <summary>
/// Base record for domain events with common properties
/// </summary>
public abstract record BaseDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public abstract string EventType { get; }
    public virtual int Version => 1;
}

/// <summary>
/// Event published when a new product is created
/// </summary>
public record ProductCreatedEvent : BaseDomainEvent
{
    public override string EventType => "product.created";
    
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public int StockQuantity { get; init; }
    public string Manufacturer { get; init; } = string.Empty;
}

/// <summary>
/// Event published when a product is updated
/// </summary>
public record ProductUpdatedEvent : BaseDomainEvent
{
    public override string EventType => "product.updated";
    
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal PreviousPrice { get; init; }
    public int StockQuantity { get; init; }
    public int PreviousStockQuantity { get; init; }
    public bool IsAvailable { get; init; }
}

/// <summary>
/// Event published when a product is deleted
/// </summary>
public record ProductDeletedEvent : BaseDomainEvent
{
    public override string EventType => "product.deleted";
    
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string DeletedBy { get; init; } = "system";
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Event published when product stock changes
/// </summary>
public record ProductStockChangedEvent : BaseDomainEvent
{
    public override string EventType => "product.stock_changed";
    
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int PreviousQuantity { get; init; }
    public int NewQuantity { get; init; }
    public int QuantityChange { get; init; }
    public string ChangeReason { get; init; } = string.Empty;
    public bool IsLowStock { get; init; }
}

/// <summary>
/// Event published when an order is placed
/// </summary>
public record OrderPlacedEvent : BaseDomainEvent
{
    public override string EventType => "order.placed";
    
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string Currency { get; init; } = "USD";
    public List<OrderItemData> Items { get; init; } = new();
    public string ShippingAddress { get; init; } = string.Empty;
}

/// <summary>
/// Event published when order payment is processed
/// </summary>
public record OrderPaymentProcessedEvent : BaseDomainEvent
{
    public override string EventType => "order.payment_processed";
    
    public string OrderId { get; init; } = string.Empty;
    public string PaymentId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public string PaymentMethod { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
    public string? FailureReason { get; init; }
}

/// <summary>
/// Event published when order is shipped
/// </summary>
public record OrderShippedEvent : BaseDomainEvent
{
    public override string EventType => "order.shipped";
    
    public string OrderId { get; init; } = string.Empty;
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public DateTime EstimatedDelivery { get; init; }
}

/// <summary>
/// Event published when a customer is created
/// </summary>
public record CustomerCreatedEvent : BaseDomainEvent
{
    public override string EventType => "customer.created";
    
    public string CustomerId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string CustomerType { get; init; } = "regular";
}

/// <summary>
/// Data transfer object for order items in events
/// </summary>
public record OrderItemData
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
}

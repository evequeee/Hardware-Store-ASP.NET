using HardwareStore.Domain.Common;
using HardwareStore.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace HardwareStore.Domain.Entities;

[BsonCollection("orders")]
public class Order : BaseEntity
{
    public string CustomerId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
    public Money TotalAmount { get; set; } = null!;
    public string Status { get; set; } = "Pending";
    public Address ShippingAddress { get; set; } = null!;
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }

    [BsonIgnore]
    public bool IsCompleted => Status == "Delivered";
}

[BsonNoId]
public class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Money UnitPrice { get; set; } = null!;

    [BsonIgnore]
    public Money TotalPrice => new Money(UnitPrice.Amount * Quantity, UnitPrice.Currency);
}

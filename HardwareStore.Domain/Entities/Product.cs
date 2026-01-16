using HardwareStore.Domain.Common;
using HardwareStore.Domain.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace HardwareStore.Domain.Entities;

[BsonCollection("products")]
public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Money Price { get; set; } = null!;
    public int StockQuantity { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;

    [BsonIgnore]
    public bool IsInStock => StockQuantity > 0;
}

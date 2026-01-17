namespace HardwareStore.AggregatorService.DTOs;

/// <summary>
/// DTO for enriched product data
/// </summary>
public class EnrichedProductDto
{
    public ProductDto Product { get; set; } = null!;
    public bool IsLowStock { get; set; }
    public decimal StockValue { get; set; }
    public DateTime RetrievedAt { get; set; }
}

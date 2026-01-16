namespace HardwareStore.AggregatorService.DTOs;

public class AggregatedDataDto
{
    public List<ProductDto> Products { get; set; } = new();
    public int TotalProducts { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public Dictionary<string, int> ProductsByCategory { get; set; } = new();
    public DateTime RetrievedAt { get; set; }
}

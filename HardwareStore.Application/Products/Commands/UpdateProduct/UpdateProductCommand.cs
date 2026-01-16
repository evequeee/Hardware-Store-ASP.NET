using MediatR;

namespace HardwareStore.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommand : IRequest<bool>
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public int StockQuantity { get; set; }
    public string Manufacturer { get; set; } = string.Empty;
    public bool IsAvailable { get; set; } = true;
}

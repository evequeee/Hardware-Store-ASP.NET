using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Domain.ValueObjects;

namespace HardwareStore.Infrastructure.Data.Seeding;

public class ProductSeeder : IDataSeeder
{
    private readonly IProductRepository _productRepository;

    public ProductSeeder(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task SeedAsync()
    {
        var existingProducts = await _productRepository.GetAllAsync();
        
        if (existingProducts.Any())
            return;

        var products = new List<Product>
        {
            new Product
            {
                Name = "Hammer",
                Description = "Heavy-duty steel hammer for construction work",
                Category = "Tools",
                Price = new Money(25.99m, "USD"),
                StockQuantity = 50,
                Manufacturer = "Stanley",
                IsAvailable = true
            },
            new Product
            {
                Name = "Screwdriver Set",
                Description = "Professional 12-piece screwdriver set with magnetic tips",
                Category = "Tools",
                Price = new Money(35.50m, "USD"),
                StockQuantity = 30,
                Manufacturer = "DeWalt",
                IsAvailable = true
            },
            new Product
            {
                Name = "Power Drill",
                Description = "Cordless 18V power drill with 2 batteries",
                Category = "Power Tools",
                Price = new Money(149.99m, "USD"),
                StockQuantity = 20,
                Manufacturer = "Makita",
                IsAvailable = true
            },
            new Product
            {
                Name = "Paint Roller",
                Description = "9-inch paint roller with extendable handle",
                Category = "Painting Supplies",
                Price = new Money(12.99m, "USD"),
                StockQuantity = 75,
                Manufacturer = "Purdy",
                IsAvailable = true
            },
            new Product
            {
                Name = "LED Work Light",
                Description = "Rechargeable LED work light, 1000 lumens",
                Category = "Lighting",
                Price = new Money(45.00m, "USD"),
                StockQuantity = 40,
                Manufacturer = "Milwaukee",
                IsAvailable = true
            }
        };

        foreach (var product in products)
        {
            await _productRepository.CreateAsync(product);
        }
    }
}

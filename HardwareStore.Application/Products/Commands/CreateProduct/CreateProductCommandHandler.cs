using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Domain.ValueObjects;
using MediatR;

namespace HardwareStore.Application.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, string>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<string> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Price = new Money(request.Price, request.Currency),
            StockQuantity = request.StockQuantity,
            Manufacturer = request.Manufacturer,
            IsAvailable = true
        };

        var createdProduct = await _productRepository.CreateAsync(product, cancellationToken);
        return createdProduct.Id;
    }
}

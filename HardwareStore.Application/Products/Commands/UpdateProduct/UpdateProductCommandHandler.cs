using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Exceptions;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Domain.ValueObjects;
using MediatR;

namespace HardwareStore.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        
        if (product == null)
            throw new NotFoundException(nameof(Product), request.Id);

        product.Name = request.Name;
        product.Description = request.Description;
        product.Category = request.Category;
        product.Price = new Money(request.Price, request.Currency);
        product.StockQuantity = request.StockQuantity;
        product.Manufacturer = request.Manufacturer;
        product.IsAvailable = request.IsAvailable;
        product.UpdatedAt = DateTime.UtcNow;

        return await _productRepository.UpdateAsync(product, cancellationToken);
    }
}

using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Exceptions;
using HardwareStore.Domain.Interfaces;
using MediatR;

namespace HardwareStore.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var exists = await _productRepository.ExistsAsync(request.Id, cancellationToken);
        
        if (!exists)
            throw new NotFoundException(nameof(Product), request.Id);

        return await _productRepository.DeleteAsync(request.Id, cancellationToken);
    }
}

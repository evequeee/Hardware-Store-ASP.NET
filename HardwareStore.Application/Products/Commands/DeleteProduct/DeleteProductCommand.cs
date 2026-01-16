using MediatR;

namespace HardwareStore.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommand : IRequest<bool>
{
    public string Id { get; set; } = string.Empty;

    public DeleteProductCommand(string id)
    {
        Id = id;
    }
}

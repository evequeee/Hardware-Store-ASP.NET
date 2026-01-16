using HardwareStore.Application.Products.Queries.GetProductById;
using MediatR;

namespace HardwareStore.Application.Products.Queries.GetProductsList;

public class GetProductsListQuery : IRequest<List<ProductDto>>
{
}

using MediatR;

namespace HardwareStore.Application.Common.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Placeholder for caching logic
        // Can be implemented with IMemoryCache or IDistributedCache
        return await next();
    }
}

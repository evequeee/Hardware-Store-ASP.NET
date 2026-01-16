using MediatR;

namespace HardwareStore.Application.Common.Behaviors;

public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Domain.Exceptions.ValidationException)
        {
            throw;
        }
        catch (Domain.Exceptions.NotFoundException)
        {
            throw;
        }
        catch (Domain.Exceptions.ConflictException)
        {
            throw;
        }
        catch (Domain.Exceptions.DomainException)
        {
            throw;
        }
        catch (Exception)
        {
            throw;
        }
    }
}

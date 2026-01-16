using HardwareStore.Domain.Exceptions;
using MongoDB.Driver;
using System.Net;
using System.Text.Json;

namespace HardwareStore.WebUI.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problemDetails = new ProblemDetailsResponse
        {
            Instance = context.Request.Path
        };

        switch (exception)
        {
            case NotFoundException notFoundException:
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                problemDetails.Title = "Not Found";
                problemDetails.Status = (int)HttpStatusCode.NotFound;
                problemDetails.Detail = notFoundException.Message;
                break;

            case Domain.Exceptions.ValidationException validationException:
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Detail = validationException.Message;
                problemDetails.Extensions = new Dictionary<string, object>
                {
                    { "errors", validationException.Errors }
                };
                break;

            case ConflictException conflictException:
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
                problemDetails.Title = "Conflict";
                problemDetails.Status = (int)HttpStatusCode.Conflict;
                problemDetails.Detail = conflictException.Message;
                break;

            case DomainException domainException:
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                problemDetails.Title = "Bad Request";
                problemDetails.Status = (int)HttpStatusCode.BadRequest;
                problemDetails.Detail = domainException.Message;
                break;

            case MongoException mongoException:
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.3";
                problemDetails.Title = "Service Unavailable";
                problemDetails.Status = (int)HttpStatusCode.ServiceUnavailable;
                problemDetails.Detail = "Database connection error";
                break;

            default:
                problemDetails.Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                problemDetails.Title = "Internal Server Error";
                problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                problemDetails.Detail = "An error occurred while processing your request";
                break;
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = problemDetails.Status;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }
}

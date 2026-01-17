using Grpc.Core;
using Grpc.Core.Interceptors;
using System.Diagnostics;

namespace HardwareStore.WebUI.GrpcServices;

/// <summary>
/// gRPC server interceptor for logging and observability
/// </summary>
public class GrpcServerLoggingInterceptor : Interceptor
{
    private readonly ILogger<GrpcServerLoggingInterceptor> _logger;

    public GrpcServerLoggingInterceptor(ILogger<GrpcServerLoggingInterceptor> logger)
    {
        _logger = logger;
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var stopwatch = Stopwatch.StartNew();
        var methodName = context.Method;
        var peer = context.Peer;

        _logger.LogInformation(
            "gRPC Request started. Method: {MethodName}, Peer: {Peer}",
            methodName, peer);

        try
        {
            var response = await continuation(request, context);
            stopwatch.Stop();

            _logger.LogInformation(
                "gRPC Request completed. Method: {MethodName}, Duration: {Duration}ms, Status: OK",
                methodName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (RpcException ex)
        {
            stopwatch.Stop();

            _logger.LogWarning(ex,
                "gRPC Request failed. Method: {MethodName}, Duration: {Duration}ms, Status: {StatusCode}, Detail: {Detail}",
                methodName, stopwatch.ElapsedMilliseconds, ex.StatusCode, ex.Status.Detail);

            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex,
                "gRPC Request error. Method: {MethodName}, Duration: {Duration}ms, Exception: {ExceptionType}",
                methodName, stopwatch.ElapsedMilliseconds, ex.GetType().Name);

            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        var methodName = context.Method;
        _logger.LogInformation("gRPC Server streaming started. Method: {MethodName}", methodName);

        await base.ServerStreamingServerHandler(request, responseStream, context, continuation);

        _logger.LogInformation("gRPC Server streaming completed. Method: {MethodName}", methodName);
    }
}

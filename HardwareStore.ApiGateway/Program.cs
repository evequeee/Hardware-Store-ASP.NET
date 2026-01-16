var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults
builder.AddServiceDefaults();

// Add YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Map ServiceDefaults endpoints
app.MapDefaultEndpoints();

app.UseCors("AllowAll");

// Use YARP Reverse Proxy
app.MapReverseProxy(proxyPipeline =>
{
    // Add CorrelationId to forwarded requests
    proxyPipeline.Use((context, next) =>
    {
        var correlationId = context.Items["CorrelationId"]?.ToString();
        if (!string.IsNullOrEmpty(correlationId))
        {
            context.Request.Headers["X-Correlation-Id"] = correlationId;
        }
        return next();
    });
});

app.Run();


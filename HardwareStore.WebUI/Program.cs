using FluentValidation;
using HardwareStore.Application.Common.Behaviors;
using HardwareStore.Application.Products.Commands.CreateProduct;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Infrastructure.Data;
using HardwareStore.Infrastructure.Data.Seeding;
using HardwareStore.Infrastructure.Repositories;
using HardwareStore.WebUI.Caching;
using HardwareStore.WebUI.Middleware;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (Serilog, OpenTelemetry, HealthChecks, ServiceDiscovery)
builder.AddServiceDefaults();

// Add Redis distributed caching
builder.AddRedisDistributedCache("redis");

// Configure MongoDB Settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Add MongoDB Context
builder.Services.AddSingleton<MongoDbContext>();

// Register Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Add Memory Cache with size limit
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024; // Maximum number of entries
    options.CompactionPercentage = 0.25; // Remove 25% when limit is reached
    options.ExpirationScanFrequency = TimeSpan.FromMinutes(1);
});

// Register Cache Services
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddSingleton<ITwoLevelCacheService, TwoLevelCacheService>();
builder.Services.AddSingleton<CacheMetrics>();

// Register Cache Warming Background Service
builder.Services.AddHostedService<CacheWarmingService>();

// Register MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});

// Register Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(HardwareStore.Application.Common.Behaviors.CachingBehavior<,>));

// Register FluentValidation Validators
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductCommand).Assembly);

// Register Data Seeders
builder.Services.AddScoped<IDataSeeder, ProductSeeder>();
builder.Services.AddScoped<IDataSeeder, CustomerSeeder>();
builder.Services.AddScoped<DatabaseSeeder>();

// Add gRPC with interceptors
builder.Services.AddGrpc(options =>
{
    options.Interceptors.Add<HardwareStore.WebUI.GrpcServices.GrpcServerLoggingInterceptor>();
    options.EnableDetailedErrors = true;
});

// Add Controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Map ServiceDefaults endpoints (health checks, CorrelationId)
app.MapDefaultEndpoints();

// Seed Database
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAllAsync();
}

// Configure the HTTP request pipeline.
app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hardware Store API V1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Map gRPC Services
app.MapGrpcService<HardwareStore.WebUI.GrpcServices.ProductGrpcServiceImpl>();

// Cache statistics endpoint for monitoring (L1 Memory Cache)
app.MapGet("/api/cache/stats", (ICacheService cacheService) =>
{
    var stats = cacheService.GetStatistics();
    return Results.Ok(stats);
}).WithName("GetCacheStatistics").WithOpenApi();

// Two-level cache statistics endpoint (L1 + L2)
app.MapGet("/api/cache/stats/two-level", (ITwoLevelCacheService twoLevelCache) =>
{
    var stats = twoLevelCache.GetStatistics();
    return Results.Ok(stats);
}).WithName("GetTwoLevelCacheStatistics").WithOpenApi();

app.Run();

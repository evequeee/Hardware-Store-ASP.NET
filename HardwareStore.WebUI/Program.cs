using FluentValidation;
using HardwareStore.Application.Common.Behaviors;
using HardwareStore.Application.Products.Commands.CreateProduct;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Infrastructure.Data;
using HardwareStore.Infrastructure.Data.Seeding;
using HardwareStore.Infrastructure.Repositories;
using HardwareStore.WebUI.Middleware;
using MediatR;

var builder = WebApplication.CreateBuilder(args);

// Add ServiceDefaults (Serilog, OpenTelemetry, HealthChecks, ServiceDiscovery)
builder.AddServiceDefaults();

// Configure MongoDB Settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Add MongoDB Context
builder.Services.AddSingleton<MongoDbContext>();

// Register Repositories
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// Register MediatR
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly);
});

// Register Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));

// Register FluentValidation Validators
builder.Services.AddValidatorsFromAssembly(typeof(CreateProductCommand).Assembly);

// Register Data Seeders
builder.Services.AddScoped<IDataSeeder, ProductSeeder>();
builder.Services.AddScoped<IDataSeeder, CustomerSeeder>();
builder.Services.AddScoped<DatabaseSeeder>();

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

app.Run();

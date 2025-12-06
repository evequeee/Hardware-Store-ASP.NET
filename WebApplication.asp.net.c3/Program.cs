using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using WebApplication.asp.net.c3.API.Middleware;
using WebApplication.asp.net.c3.BLL.Interfaces;
using WebApplication.asp.net.c3.BLL.Mapping;
using WebApplication.asp.net.c3.BLL.Services;
using WebApplication.asp.net.c3.DAL.Interfaces;
using WebApplication.asp.net.c3.DAL.Repositories;
using WebApplication.asp.net.c3.Data;

namespace WebApplication.asp.net.c3;

public class Program
{
    public static void Main(string[] args)
    {
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
            .Enrich.FromLogContext()
            .CreateLogger();

        try
        {
            Log.Information("Starting Hardware Store API");
            
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog();

            // Add services to the container
            ConfigureServices(builder.Services, builder.Configuration);

            var app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigureMiddleware(app);

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Database Context - Entity Framework Core with PostgreSQL
        services.AddDbContext<HardwareStoreDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Controllers
        services.AddControllers();

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<Program>();

        // Dependency Injection - DAL (Unit of Work pattern)
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Dependency Injection - DAL (Repositories)
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        // Dependency Injection - BLL (Business Logic Services)
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IProductService, ProductService>();

        // API Documentation
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Hardware Store API - Product Catalog",
                Version = "v1",
                Description = "Three-tier architecture API using Entity Framework Core with Code-First approach and Fluent API",
                Contact = new Microsoft.OpenApi.Models.OpenApiContact
                {
                    Name = "Hardware Store Team"
                }
            });

            // Enable XML comments if available
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        // CORS (if needed)
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
        });
    }

    private static void ConfigureMiddleware(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        // Global exception handler
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        // Enable Swagger
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hardware Store API v1");
                c.RoutePrefix = string.Empty; // Swagger at root
            });
        }

        // Serilog request logging
        app.UseSerilogRequestLogging();

        app.UseHttpsRedirection();

        app.UseCors("AllowAll");

        app.UseAuthorization();

        app.MapControllers();
    }
}
using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.Data;

namespace WebApplication.asp.net.c3
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Використовуємо повне ім'я типу для уникнення конфлікту з namespace
            var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

            // Add services to the container.

            // Додаємо DbContext з підключенням до PostgreSQL
            builder.Services.AddDbContext<ProductCatalogDbContext>(options =>
                options.UseNpgsql(
                    builder.Configuration.GetConnectionString("ProductCatalogDb"),
                    npgsqlOptions =>
                    {
                        npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorCodesToAdd: null
                        );
                        npgsqlOptions.CommandTimeout(60);
                    }
                )
                .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
                .EnableDetailedErrors(builder.Environment.IsDevelopment())
            );

            builder.Services.AddControllers();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Hardware Store API - Product Catalog",
                    Version = "v1",
                    Description = "API для каталогу комп'ютерних комплектуючих",
                    Contact = new Microsoft.OpenApi.Models.OpenApiContact
                    {
                        Name = "Hardware Store Team"
                    }
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hardware Store API v1");
                    c.RoutePrefix = string.Empty; // Swagger на головній сторінці
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
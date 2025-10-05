using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.Data
{
    public class ProductCatalogDbContext : DbContext
    {
        public ProductCatalogDbContext(DbContextOptions<ProductCatalogDbContext> options) : base(options)
        {
        }

        // DbSets для всіх сутностей
        public DbSet<Category> Categories { get; set; }
        public DbSet<Brand> Brands { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductAttribute> ProductAttributes { get; set; }
        public DbSet<ProductReview> ProductReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Конфігурація через Fluent API
            ConfigureBaseEntity(modelBuilder);
            ConfigureCategory(modelBuilder);
            ConfigureBrand(modelBuilder);
            ConfigureProduct(modelBuilder);
            ConfigureProductImage(modelBuilder);
            ConfigureProductAttribute(modelBuilder);
            ConfigureProductReview(modelBuilder);

            // Seed даних
            SeedData(modelBuilder);
        }

        private void ConfigureBaseEntity(ModelBuilder modelBuilder)
        {
            // Конфігурація для всіх сутностей, що наслідують BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("GETUTCDATE()");

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("IsDeleted")
                        .HasDefaultValue(false);

                    // Фільтри для м'якого видалення для кожної сутності окремо
                    modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
                    modelBuilder.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted);
                    modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
                    modelBuilder.Entity<ProductImage>().HasQueryFilter(e => !e.IsDeleted);
                    modelBuilder.Entity<ProductAttribute>().HasQueryFilter(e => !e.IsDeleted);
                    modelBuilder.Entity<ProductReview>().HasQueryFilter(e => !e.IsDeleted);
                }
            }
        }

        private void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.Id)
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(5000);
                entity.Property(e => e.ImageUrl).HasMaxLength(300);
                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("IX_Categories_Name");
                entity.HasIndex(e => e.ParentCategory).HasDatabaseName("IX_Categories_ParentCategoryId");
                entity.HasIndex(e => new { e.IsActive, e.SortOrder }).HasDatabaseName("IX_Categories_IsActive_SortOrder");
                //Ієрархія
                entity.HasOne(e => e.ParentCategory)
                      .WithMany(e => e.SubCategories)
                      .HasForeignKey(e => e.ParentCategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(e => e.Products)
                      .WithOne(e => e.Category)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
            });
        }


    }
}
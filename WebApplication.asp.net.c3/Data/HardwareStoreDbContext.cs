using Microsoft.EntityFrameworkCore;
using WebApplication.asp.net.c3.Models;

namespace WebApplication.asp.net.c3.Data;

public class HardwareStoreDbContext : DbContext
{
    public HardwareStoreDbContext(DbContextOptions<HardwareStoreDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Brand> Brands { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductAttribute> ProductAttributes { get; set; }
    public DbSet<ProductReview> ProductReviews { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply Fluent API configurations
        ConfigureBaseEntity(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureBrand(modelBuilder);
        ConfigureProduct(modelBuilder);
        ConfigureProductImage(modelBuilder);
        ConfigureProductAttribute(modelBuilder);
        ConfigureProductReview(modelBuilder);

        // Apply Seed Data
        SeedData(modelBuilder);
    }

    private void ConfigureBaseEntity(ModelBuilder modelBuilder)
    {
        // Configure common properties for all entities inheriting from BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Default value for CreatedAt
                modelBuilder.Entity(entityType.ClrType)
                    .Property("CreatedAt")
                    .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                // Default value for IsDeleted
                modelBuilder.Entity(entityType.ClrType)
                    .Property("IsDeleted")
                    .HasDefaultValue(false);

                // Query filter to exclude soft-deleted entities
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, "IsDeleted");
                var filterExpression = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filterExpression);
            }
        }
    }

    private void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(entity =>
        {
            // Table name
            entity.ToTable("categories");

            // Primary key
            entity.HasKey(e => e.Id);

            // Properties configuration
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(5000);

            entity.Property(e => e.ImageUrl)
                .HasMaxLength(300);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            // Unique constraint
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("ix_categories_name");

            // Indexes
            entity.HasIndex(e => e.ParentCategoryId)
                .HasDatabaseName("ix_categories_parent_category_id");

            entity.HasIndex(e => new { e.IsActive, e.SortOrder })
                .HasDatabaseName("ix_categories_is_active_sort_order");

            // Self-referencing relationship
            entity.HasOne(e => e.ParentCategory)
                .WithMany(e => e.SubCategories)
                .HasForeignKey(e => e.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-many with Products
            entity.HasMany(e => e.Products)
                .WithOne(e => e.Category)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureBrand(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>(entity =>
        {
            // Table name
            entity.ToTable("brands");

            // Primary key
            entity.HasKey(e => e.Id);

            // Properties configuration
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .HasMaxLength(500);

            entity.Property(e => e.LogoUrl)
                .HasMaxLength(300);

            entity.Property(e => e.WebsiteUrl)
                .HasMaxLength(200);

            entity.Property(e => e.Country)
                .HasMaxLength(100);

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Unique constraint
            entity.HasIndex(e => e.Name)
                .IsUnique()
                .HasDatabaseName("ix_brands_name");

            // Index
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("ix_brands_is_active");

            // One-to-many with Products
            entity.HasMany(e => e.Products)
                .WithOne(e => e.Brand)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private void ConfigureProduct(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            // Table name
            entity.ToTable("products");

            // Primary key
            entity.HasKey(e => e.Id);

            // Properties configuration
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(150);

            entity.Property(e => e.Sku)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Price)
                .IsRequired()
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.DiscountPrice)
                .HasColumnType("decimal(18,2)");

            entity.Property(e => e.StockQuantity)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.IsAvailable)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.IsFeatured)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.AverageRating)
                .HasColumnType("double precision");

            entity.Property(e => e.ReviewCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.Tags)
                .HasMaxLength(500);

            // Unique constraint
            entity.HasIndex(e => e.Sku)
                .IsUnique()
                .HasDatabaseName("ix_products_sku");

            // Indexes
            entity.HasIndex(e => e.CategoryId)
                .HasDatabaseName("ix_products_category_id");

            entity.HasIndex(e => e.BrandId)
                .HasDatabaseName("ix_products_brand_id");

            entity.HasIndex(e => new { e.IsAvailable, e.IsFeatured })
                .HasDatabaseName("ix_products_is_available_is_featured");

            entity.HasIndex(e => e.Price)
                .HasDatabaseName("ix_products_price");

            // Relationships configured in ConfigureCategory and ConfigureBrand
        });
    }

    private void ConfigureProductImage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.ToTable("product_images");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.ImageUrl)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.AltText)
                .HasMaxLength(200);

            entity.Property(e => e.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.SortOrder)
                .IsRequired()
                .HasDefaultValue(0);

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("ix_product_images_product_id");

            entity.HasOne(e => e.Product)
                .WithMany(e => e.ProductImages)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureProductAttribute(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductAttribute>(entity =>
        {
            entity.ToTable("product_attributes");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Value)
                .IsRequired()
                .HasMaxLength(500);

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("ix_product_attributes_product_id");

            entity.HasIndex(e => e.Name)
                .HasDatabaseName("ix_product_attributes_attribute_name");

            entity.HasOne(e => e.Product)
                .WithMany(e => e.ProductAttributes)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureProductReview(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProductReview>(entity =>
        {
            entity.ToTable("product_reviews");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.CustomerName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Comment)
                .HasMaxLength(2000);

            entity.Property(e => e.Rating)
                .IsRequired();

            entity.Property(e => e.IsApproved)
                .IsRequired()
                .HasDefaultValue(false);

            entity.HasIndex(e => e.ProductId)
                .HasDatabaseName("ix_product_reviews_product_id");

            entity.HasIndex(e => e.Rating)
                .HasDatabaseName("ix_product_reviews_rating");

            entity.HasOne(e => e.Product)
                .WithMany(e => e.ProductReviews)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed Categories
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Процесори", Description = "CPU для ПК та серверів", IsActive = true, SortOrder = 1, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Category { Id = 2, Name = "Відеокарти", Description = "GPU для ігор та професійних задач", IsActive = true, SortOrder = 2, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Category { Id = 3, Name = "Материнські плати", Description = "Motherboards для різних платформ", IsActive = true, SortOrder = 3, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Category { Id = 4, Name = "Оперативна пам'ять", Description = "RAM модулі DDR4/DDR5", IsActive = true, SortOrder = 4, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Category { Id = 5, Name = "Накопичувачі", Description = "SSD, HDD, NVMe диски", IsActive = true, SortOrder = 5, CreatedAt = DateTime.UtcNow, IsDeleted = false }
        );

        // Seed Brands
        modelBuilder.Entity<Brand>().HasData(
            new Brand { Id = 1, Name = "Intel", Description = "Світовий лідер у виробництві процесорів", Country = "USA", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Brand { Id = 2, Name = "AMD", Description = "Високопродуктивні процесори та відеокарти", Country = "USA", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Brand { Id = 3, Name = "NVIDIA", Description = "Лідер у виробництві графічних процесорів", Country = "USA", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Brand { Id = 4, Name = "ASUS", Description = "Материнські плати та периферія", Country = "Taiwan", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Brand { Id = 5, Name = "Corsair", Description = "Оперативна пам'ять та периферія", Country = "USA", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Brand { Id = 6, Name = "Samsung", Description = "Накопичувачі та електроніка", Country = "South Korea", IsActive = true, CreatedAt = DateTime.UtcNow, IsDeleted = false }
        );

        // Seed Products
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Intel Core i9-14900K", Sku = "CPU-INT-I914900K", Description = "24-ядерний процесор для високопродуктивних систем", CategoryId = 1, BrandId = 1, Price = 25999m, StockQuantity = 15, IsAvailable = true, IsFeatured = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Product { Id = 2, Name = "AMD Ryzen 9 7950X", Sku = "CPU-AMD-R97950X", Description = "16-ядерний процесор з підтримкою DDR5", CategoryId = 1, BrandId = 2, Price = 23499m, DiscountPrice = 21999m, StockQuantity = 20, IsAvailable = true, IsFeatured = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Product { Id = 3, Name = "NVIDIA RTX 4090", Sku = "GPU-NVD-RTX4090", Description = "Топова відеокарта для 4K гемінгу", CategoryId = 2, BrandId = 3, Price = 79999m, StockQuantity = 8, IsAvailable = true, IsFeatured = true, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Product { Id = 4, Name = "ASUS ROG STRIX Z790-E", Sku = "MB-ASUS-Z790E", Description = "Материнська плата для Intel 13/14 gen", CategoryId = 3, BrandId = 4, Price = 15999m, StockQuantity = 12, IsAvailable = true, IsFeatured = false, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Product { Id = 5, Name = "Corsair Vengeance DDR5 32GB", Sku = "RAM-COR-VEN32", Description = "Оперативна пам'ять DDR5-6000 MHz", CategoryId = 4, BrandId = 5, Price = 5499m, StockQuantity = 30, IsAvailable = true, IsFeatured = false, CreatedAt = DateTime.UtcNow, IsDeleted = false },
            new Product { Id = 6, Name = "Samsung 990 PRO 2TB", Sku = "SSD-SAM-990PRO2TB", Description = "Швидкий NVMe SSD накопичувач", CategoryId = 5, BrandId = 6, Price = 7999m, DiscountPrice = 7299m, StockQuantity = 25, IsAvailable = true, IsFeatured = true, CreatedAt = DateTime.UtcNow, IsDeleted = false }
        );
    }
}

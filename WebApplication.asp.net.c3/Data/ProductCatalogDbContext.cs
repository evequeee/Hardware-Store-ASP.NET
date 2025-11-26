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
            // Конфігурація загальних властивостей
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    // PostgreSQL використовує NOW() AT TIME ZONE 'UTC' замість GETUTCDATE()
                    modelBuilder.Entity(entityType.ClrType)
                        .Property("CreatedAt")
                        .HasDefaultValueSql("NOW() AT TIME ZONE 'UTC'");

                    modelBuilder.Entity(entityType.ClrType)
                        .Property("IsDeleted")
                        .HasDefaultValue(false);
                }
            }

            // Фільтри для м'якого видалення для кожної сутності окремо
            modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Brand>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ProductImage>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ProductAttribute>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ProductReview>().HasQueryFilter(e => !e.IsDeleted);
        }

        private void ConfigureCategory(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>(entity =>
            {
                entity.ToTable("categories");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(5000);
                entity.Property(e => e.ImageUrl).HasMaxLength(300);

                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("ix_categories_name");
                entity.HasIndex(e => e.ParentCategoryId).HasDatabaseName("ix_categories_parent_category_id");
                entity.HasIndex(e => new { e.IsActive, e.SortOrder }).HasDatabaseName("ix_categories_is_active_sort_order");

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

        private void ConfigureBrand(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Brand>(entity =>
            {
                entity.ToTable("brands");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.LogoUrl).HasMaxLength(300);
                entity.Property(e => e.Website).HasMaxLength(200);

                entity.HasIndex(e => e.Name).IsUnique().HasDatabaseName("ix_brands_name");
                entity.HasIndex(e => e.IsActive).HasDatabaseName("ix_brands_is_active");

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
                entity.ToTable("products");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
                entity.Property(e => e.SKU).HasMaxLength(50);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.DetailedDescription).HasMaxLength(5000);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.DiscountedPrice).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Tags).HasMaxLength(500);

                entity.HasIndex(e => e.SKU).IsUnique().HasDatabaseName("ix_products_sku").HasFilter("\"SKU\" IS NOT NULL");
                entity.HasIndex(e => e.Name).HasDatabaseName("ix_products_name");
                entity.HasIndex(e => new { e.CategoryId, e.IsActive }).HasDatabaseName("ix_products_category_id_is_active");
                entity.HasIndex(e => new { e.BrandId, e.IsActive }).HasDatabaseName("ix_products_brand_id_is_active");
                entity.HasIndex(e => new { e.Price, e.IsActive }).HasDatabaseName("ix_products_price_is_active");
                entity.HasIndex(e => e.IsFeatured).HasDatabaseName("ix_products_is_featured");

                entity.HasCheckConstraint("ck_products_price_positive", "\"Price\" > 0");
                entity.HasCheckConstraint("ck_products_discounted_price_positive", "\"DiscountedPrice\" IS NULL OR \"DiscountedPrice\" > 0");
                entity.HasCheckConstraint("ck_products_stock_quantity_non_negative", "\"StockQuantity\" >= 0");
                entity.HasCheckConstraint("ck_products_average_rating_range", "\"AverageRating\" IS NULL OR (\"AverageRating\" >= 0 AND \"AverageRating\" <= 5)");
            });
        }

        private void ConfigureProductImage(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.ToTable("product_images");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.ImageUrl).IsRequired().HasMaxLength(500);
                entity.Property(e => e.AltText).HasMaxLength(200);

                entity.HasIndex(e => new { e.ProductId, e.IsPrimary }).HasDatabaseName("ix_product_images_product_id_is_primary");
                entity.HasIndex(e => new { e.ProductId, e.SortOrder }).HasDatabaseName("ix_product_images_product_id_sort_order");

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

                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Value).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Unit).HasMaxLength(50);

                entity.HasIndex(e => new { e.ProductId, e.Name }).HasDatabaseName("ix_product_attributes_product_id_name");
                entity.HasIndex(e => new { e.ProductId, e.SortOrder }).HasDatabaseName("ix_product_attributes_product_id_sort_order");

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

                entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CustomerEmail).HasMaxLength(200);
                entity.Property(e => e.Title).HasMaxLength(200);
                entity.Property(e => e.Comment).HasMaxLength(2000);
                entity.Property(e => e.ApprovedBy).HasMaxLength(100);

                entity.HasIndex(e => new { e.ProductId, e.IsApproved }).HasDatabaseName("ix_product_reviews_product_id_is_approved");
                entity.HasIndex(e => e.Rating).HasDatabaseName("ix_product_reviews_rating");

                entity.HasCheckConstraint("ck_product_reviews_rating_range", "\"Rating\" >= 1 AND \"Rating\" <= 5");

                entity.HasOne(e => e.Product)
                    .WithMany(e => e.ProductReviews)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            var createdAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Процесори", Description = "Центральні процесори для ПК", IsActive = true, SortOrder = 1, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 2, Name = "Материнські плати", Description = "Материнські плати для різних платформ", IsActive = true, SortOrder = 2, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 3, Name = "Відеокарти", Description = "Графічні процесори та відеокарти", IsActive = true, SortOrder = 3, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 4, Name = "Оперативна пам'ять", Description = "DDR4 та DDR5 модулі пам'яті", IsActive = true, SortOrder = 4, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 5, Name = "Накопичувачі", Description = "SSD та HDD накопичувачі", IsActive = true, SortOrder = 5, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 6, Name = "SSD накопичувачі", Description = "Твердотільні накопичувачі", IsActive = true, SortOrder = 1, CreatedAt = createdAt, ParentCategoryId = 5 },
                new Category { Id = 7, Name = "HDD накопичувачі", Description = "Жорсткі диски", IsActive = true, SortOrder = 2, CreatedAt = createdAt, ParentCategoryId = 5 },
                new Category { Id = 8, Name = "Блоки живлення", Description = "Блоки живлення для ПК", IsActive = true, SortOrder = 6, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 9, Name = "Корпуси", Description = "Корпуси для збірки ПК", IsActive = true, SortOrder = 7, CreatedAt = createdAt, ParentCategoryId = null },
                new Category { Id = 10, Name = "Охолодження", Description = "Системи охолодження для ПК", IsActive = true, SortOrder = 8, CreatedAt = createdAt, ParentCategoryId = null }
            );

            modelBuilder.Entity<Brand>().HasData(
                new Brand { Id = 1, Name = "Intel", Description = "Виробник процесорів", Website = "https://www.intel.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 2, Name = "AMD", Description = "Лідер у виробництві процесорів", Website = "https://www.amd.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 3, Name = "NVIDIA", Description = "Лідер у виробництві відеокарт", Website = "https://www.nvidia.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 4, Name = "ASUS", Description = "Виробник материнських плат та відеокарт", Website = "https://www.asus.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 5, Name = "MSI", Description = "Виробник материнських плат, відеокарт та периферії", Website = "https://www.msi.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 6, Name = "Gigabyte", Description = "Виробник материнських плат та відеокарт", Website = "https://www.gigabyte.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 7, Name = "Corsair", Description = "Виробник пам'яті, БЖ та периферії", Website = "https://www.corsair.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 8, Name = "Kingston", Description = "Виробник пам'яті та накопичувачів", Website = "https://www.kingston.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 9, Name = "Samsung", Description = "Виробник SSD накопичувачів та пам'яті", Website = "https://www.samsung.com", IsActive = true, CreatedAt = createdAt },
                new Brand { Id = 10, Name = "Western Digital", Description = "Виробник HDD та SSD накопичувачів", Website = "https://www.westerndigital.com", IsActive = true, CreatedAt = createdAt }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Intel Core i9-14900K", SKU = "CPU-INTEL-I9-14900K", Description = "24-ядерний процесор для високопродуктивних систем", DetailedDescription = "Intel Core i9-14900K - флагманський процесор 14-го покоління з 24 ядрами (8P+16E), 32 потоками, базовою частотою 3.2 GHz та турбо до 6.0 GHz. Сокет LGA1700, TDP 125W (253W max).", Price = 24999.00m, DiscountedPrice = 23499.00m, StockQuantity = 15, IsActive = true, IsFeatured = true, AverageRating = 4.8, ReviewCount = 47, Tags = "cpu,intel,gaming,workstation,lga1700", CategoryId = 1, BrandId = 1, CreatedAt = createdAt },
                new Product { Id = 2, Name = "AMD Ryzen 9 7950X", SKU = "CPU-AMD-R9-7950X", Description = "16-ядерний процесор на архітектурі Zen 4", DetailedDescription = "AMD Ryzen 9 7950X - топовий 16-ядерний процесор з 32 потоками, базовою частотою 4.5 GHz та Boost до 5.7 GHz. Сокет AM5, TDP 170W, підтримка DDR5 та PCIe 5.0.", Price = 22999.00m, StockQuantity = 22, IsActive = true, IsFeatured = true, AverageRating = 4.9, ReviewCount = 63, Tags = "cpu,amd,gaming,workstation,am5,zen4", CategoryId = 1, BrandId = 2, CreatedAt = createdAt },
                new Product { Id = 3, Name = "Intel Core i5-14600K", SKU = "CPU-INTEL-I5-14600K", Description = "14-ядерний процесор середнього рівня", DetailedDescription = "Intel Core i5-14600K - 14-ядерний процесор (6P+8E) з 20 потоками, базовою частотою 3.5 GHz та турбо до 5.3 GHz. Сокет LGA1700, TDP 125W.", Price = 12999.00m, DiscountedPrice = 11999.00m, StockQuantity = 35, IsActive = true, IsFeatured = false, AverageRating = 4.7, ReviewCount = 89, Tags = "cpu,intel,gaming,mainstream,lga1700", CategoryId = 1, BrandId = 1, CreatedAt = createdAt },
                new Product { Id = 4, Name = "ASUS ROG MAXIMUS Z790 HERO", SKU = "MB-ASUS-Z790-HERO", Description = "Преміум материнська плата для Intel 14-го покоління", DetailedDescription = "ASUS ROG MAXIMUS Z790 HERO - топова материнська плата на чіпсеті Z790, сокет LGA1700. Підтримка DDR5-7800+, PCIe 5.0, WiFi 6E, 2.5G LAN, RGB підсвічування AURA Sync.", Price = 18999.00m, StockQuantity = 12, IsActive = true, IsFeatured = true, AverageRating = 4.9, ReviewCount = 34, Tags = "motherboard,asus,z790,lga1700,ddr5,gaming", CategoryId = 2, BrandId = 4, CreatedAt = createdAt },
                new Product { Id = 5, Name = "MSI MAG B650 TOMAHAWK WIFI", SKU = "MB-MSI-B650-TOMAHAWK", Description = "Материнська плата для AMD Ryzen 7000", DetailedDescription = "MSI MAG B650 TOMAHAWK WIFI - материнська плата на чіпсеті B650, сокет AM5. Підтримка DDR5-6400+, PCIe 5.0, WiFi 6E, 2.5G LAN, RGB підсвічування Mystic Light.", Price = 9499.00m, DiscountedPrice = 8999.00m, StockQuantity = 28, IsActive = true, IsFeatured = true, AverageRating = 4.6, ReviewCount = 56, Tags = "motherboard,msi,b650,am5,ddr5,gaming", CategoryId = 2, BrandId = 5, CreatedAt = createdAt },
                new Product { Id = 6, Name = "ASUS ROG Strix GeForce RTX 4090", SKU = "GPU-ASUS-RTX4090-STRIX", Description = "Найпотужніша геймерська відеокарта", DetailedDescription = "ASUS ROG Strix RTX 4090 - флагманська відеокарта на базі NVIDIA Ada Lovelace. 24GB GDDR6X, 16384 CUDA ядер, частота до 2640 MHz, 3x DisplayPort 1.4a, 2x HDMI 2.1, RGB підсвічування AURA Sync.", Price = 79999.00m, StockQuantity = 5, IsActive = true, IsFeatured = true, AverageRating = 5.0, ReviewCount = 28, Tags = "gpu,nvidia,rtx4090,gaming,4k,raytracing", CategoryId = 3, BrandId = 4, CreatedAt = createdAt }
            );

            modelBuilder.Entity<ProductImage>().HasData(
                new ProductImage { Id = 1, ProductId = 1, ImageUrl = "/images/products/intel-i9-14900k-1.jpg", AltText = "Intel Core i9-14900K", IsPrimary = true, SortOrder = 1, CreatedAt = createdAt },
                new ProductImage { Id = 2, ProductId = 2, ImageUrl = "/images/products/amd-ryzen9-7950x-1.jpg", AltText = "AMD Ryzen 9 7950X", IsPrimary = true, SortOrder = 1, CreatedAt = createdAt },
                new ProductImage { Id = 3, ProductId = 4, ImageUrl = "/images/products/asus-z790-hero-1.jpg", AltText = "ASUS ROG MAXIMUS Z790 HERO", IsPrimary = true, SortOrder = 1, CreatedAt = createdAt },
                new ProductImage { Id = 4, ProductId = 6, ImageUrl = "/images/products/asus-rtx4090-strix-1.jpg", AltText = "ASUS ROG Strix RTX 4090", IsPrimary = true, SortOrder = 1, CreatedAt = createdAt }
            );

            modelBuilder.Entity<ProductAttribute>().HasData(
                new ProductAttribute { Id = 1, ProductId = 1, Name = "Ядра / Потоки", Value = "24 (8P+16E) / 32", SortOrder = 1, CreatedAt = createdAt },
                new ProductAttribute { Id = 2, ProductId = 1, Name = "Базова частота", Value = "3.2", Unit = "GHz", SortOrder = 2, CreatedAt = createdAt },
                new ProductAttribute { Id = 3, ProductId = 2, Name = "Ядра / Потоки", Value = "16 / 32", SortOrder = 1, CreatedAt = createdAt },
                new ProductAttribute { Id = 4, ProductId = 2, Name = "Сокет", Value = "AM5", SortOrder = 2, CreatedAt = createdAt }
            );

            modelBuilder.Entity<ProductReview>().HasData(
                new ProductReview { Id = 1, ProductId = 1, CustomerName = "Олександр Коваленко", CustomerEmail = "alex@example.com", Rating = 5, Title = "Звір процесор!", Comment = "Використовую для стрімінгу. Справляється на ура!", IsApproved = true, ApprovedAt = createdAt.AddDays(1), ApprovedBy = "admin", CreatedAt = createdAt },
                new ProductReview { Id = 2, ProductId = 2, CustomerName = "Дмитро Петренко", CustomerEmail = "dmytro@example.com", Rating = 5, Title = "Ідеальний для workstation", Comment = "16 ядер Zen 4 - це потужність!", IsApproved = true, ApprovedAt = createdAt.AddDays(2), ApprovedBy = "admin", CreatedAt = createdAt.AddDays(1) }
            );
        }
    }
}
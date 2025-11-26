using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication.asp.net.c3.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_categories_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SKU = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DetailedDescription = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountedPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: false),
                    BrandId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                    table.CheckConstraint("ck_products_average_rating_range", "\"AverageRating\" IS NULL OR (\"AverageRating\" >= 0 AND \"AverageRating\" <= 5)");
                    table.CheckConstraint("ck_products_discounted_price_positive", "\"DiscountedPrice\" IS NULL OR \"DiscountedPrice\" > 0");
                    table.CheckConstraint("ck_products_price_positive", "\"Price\" > 0");
                    table.CheckConstraint("ck_products_stock_quantity_non_negative", "\"StockQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_products_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_attributes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_attributes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_attributes_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_images_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_reviews",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW() AT TIME ZONE 'UTC'"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_reviews", x => x.Id);
                    table.CheckConstraint("ck_product_reviews_rating_range", "\"Rating\" >= 1 AND \"Rating\" <= 5");
                    table.ForeignKey(
                        name: "FK_product_reviews_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "brands",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "IsActive", "LogoUrl", "Name", "UpdatedAt", "UpdatedBy", "Website" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник процесорів", true, null, "Intel", null, null, "https://www.intel.com" },
                    { 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Лідер у виробництві процесорів", true, null, "AMD", null, null, "https://www.amd.com" },
                    { 3L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Лідер у виробництві відеокарт", true, null, "NVIDIA", null, null, "https://www.nvidia.com" },
                    { 4L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник материнських плат та відеокарт", true, null, "ASUS", null, null, "https://www.asus.com" },
                    { 5L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник материнських плат, відеокарт та периферії", true, null, "MSI", null, null, "https://www.msi.com" },
                    { 6L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник материнських плат та відеокарт", true, null, "Gigabyte", null, null, "https://www.gigabyte.com" },
                    { 7L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник пам'яті, БЖ та периферії", true, null, "Corsair", null, null, "https://www.corsair.com" },
                    { 8L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник пам'яті та накопичувачів", true, null, "Kingston", null, null, "https://www.kingston.com" },
                    { 9L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник SSD накопичувачів та пам'яті", true, null, "Samsung", null, null, "https://www.samsung.com" },
                    { 10L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Виробник HDD та SSD накопичувачів", true, null, "Western Digital", null, null, "https://www.westerndigital.com" }
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "ImageUrl", "IsActive", "Name", "ParentCategoryId", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Центральні процесори для ПК", null, true, "Процесори", null, 1, null, null },
                    { 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Материнські плати для різних платформ", null, true, "Материнські плати", null, 2, null, null },
                    { 3L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Графічні процесори та відеокарти", null, true, "Відеокарти", null, 3, null, null },
                    { 4L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "DDR4 та DDR5 модулі пам'яті", null, true, "Оперативна пам'ять", null, 4, null, null },
                    { 5L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "SSD та HDD накопичувачі", null, true, "Накопичувачі", null, 5, null, null },
                    { 8L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Блоки живлення для ПК", null, true, "Блоки живлення", null, 6, null, null },
                    { 9L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Корпуси для збірки ПК", null, true, "Корпуси", null, 7, null, null },
                    { 10L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Системи охолодження для ПК", null, true, "Охолодження", null, 8, null, null },
                    { 6L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Твердотільні накопичувачі", null, true, "SSD накопичувачі", 5L, 1, null, null },
                    { 7L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Жорсткі диски", null, true, "HDD накопичувачі", 5L, 2, null, null }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "AverageRating", "BrandId", "CategoryId", "CreatedAt", "CreatedBy", "Description", "DetailedDescription", "DiscountedPrice", "IsActive", "IsFeatured", "Name", "Price", "ReviewCount", "SKU", "StockQuantity", "Tags", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, 4.7999999999999998, 1L, 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "24-ядерний процесор для високопродуктивних систем", "Intel Core i9-14900K - флагманський процесор 14-го покоління з 24 ядрами (8P+16E), 32 потоками, базовою частотою 3.2 GHz та турбо до 6.0 GHz. Сокет LGA1700, TDP 125W (253W max).", 23499.00m, true, true, "Intel Core i9-14900K", 24999.00m, 47, "CPU-INTEL-I9-14900K", 15, "cpu,intel,gaming,workstation,lga1700", null, null },
                    { 2L, 4.9000000000000004, 2L, 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "16-ядерний процесор на архітектурі Zen 4", "AMD Ryzen 9 7950X - топовий 16-ядерний процесор з 32 потоками, базовою частотою 4.5 GHz та Boost до 5.7 GHz. Сокет AM5, TDP 170W, підтримка DDR5 та PCIe 5.0.", null, true, true, "AMD Ryzen 9 7950X", 22999.00m, 63, "CPU-AMD-R9-7950X", 22, "cpu,amd,gaming,workstation,am5,zen4", null, null },
                    { 3L, 4.7000000000000002, 1L, 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "14-ядерний процесор середнього рівня", "Intel Core i5-14600K - 14-ядерний процесор (6P+8E) з 20 потоками, базовою частотою 3.5 GHz та турбо до 5.3 GHz. Сокет LGA1700, TDP 125W.", 11999.00m, true, false, "Intel Core i5-14600K", 12999.00m, 89, "CPU-INTEL-I5-14600K", 35, "cpu,intel,gaming,mainstream,lga1700", null, null },
                    { 4L, 4.9000000000000004, 4L, 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Преміум материнська плата для Intel 14-го покоління", "ASUS ROG MAXIMUS Z790 HERO - топова материнська плата на чіпсеті Z790, сокет LGA1700. Підтримка DDR5-7800+, PCIe 5.0, WiFi 6E, 2.5G LAN, RGB підсвічування AURA Sync.", null, true, true, "ASUS ROG MAXIMUS Z790 HERO", 18999.00m, 34, "MB-ASUS-Z790-HERO", 12, "motherboard,asus,z790,lga1700,ddr5,gaming", null, null },
                    { 5L, 4.5999999999999996, 5L, 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Материнська плата для AMD Ryzen 7000", "MSI MAG B650 TOMAHAWK WIFI - материнська плата на чіпсеті B650, сокет AM5. Підтримка DDR5-6400+, PCIe 5.0, WiFi 6E, 2.5G LAN, RGB підсвічування Mystic Light.", 8999.00m, true, true, "MSI MAG B650 TOMAHAWK WIFI", 9499.00m, 56, "MB-MSI-B650-TOMAHAWK", 28, "motherboard,msi,b650,am5,ddr5,gaming", null, null },
                    { 6L, 5.0, 4L, 3L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Найпотужніша геймерська відеокарта", "ASUS ROG Strix RTX 4090 - флагманська відеокарта на базі NVIDIA Ada Lovelace. 24GB GDDR6X, 16384 CUDA ядер, частота до 2640 MHz, 3x DisplayPort 1.4a, 2x HDMI 2.1, RGB підсвічування AURA Sync.", null, true, true, "ASUS ROG Strix GeForce RTX 4090", 79999.00m, 28, "GPU-ASUS-RTX4090-STRIX", 5, "gpu,nvidia,rtx4090,gaming,4k,raytracing", null, null }
                });

            migrationBuilder.InsertData(
                table: "product_attributes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Name", "ProductId", "SortOrder", "Unit", "UpdatedAt", "UpdatedBy", "Value" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ядра / Потоки", 1L, 1, null, null, null, "24 (8P+16E) / 32" },
                    { 2L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Базова частота", 1L, 2, "GHz", null, null, "3.2" },
                    { 3L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Ядра / Потоки", 2L, 1, null, null, null, "16 / 32" },
                    { 4L, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Сокет", 2L, 2, null, null, null, "AM5" }
                });

            migrationBuilder.InsertData(
                table: "product_images",
                columns: new[] { "Id", "AltText", "CreatedAt", "CreatedBy", "ImageUrl", "IsPrimary", "ProductId", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, "Intel Core i9-14900K", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "/images/products/intel-i9-14900k-1.jpg", true, 1L, 1, null, null },
                    { 2L, "AMD Ryzen 9 7950X", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "/images/products/amd-ryzen9-7950x-1.jpg", true, 2L, 1, null, null },
                    { 3L, "ASUS ROG MAXIMUS Z790 HERO", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "/images/products/asus-z790-hero-1.jpg", true, 4L, 1, null, null },
                    { 4L, "ASUS ROG Strix RTX 4090", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "/images/products/asus-rtx4090-strix-1.jpg", true, 6L, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "product_reviews",
                columns: new[] { "Id", "ApprovedAt", "ApprovedBy", "Comment", "CreatedAt", "CreatedBy", "CustomerEmail", "CustomerName", "IsApproved", "ProductId", "Rating", "Title", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1L, new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "admin", "Використовую для стрімінгу. Справляється на ура!", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "alex@example.com", "Олександр Коваленко", true, 1L, 5, "Звір процесор!", null, null },
                    { 2L, new DateTime(2024, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "admin", "16 ядер Zen 4 - це потужність!", new DateTime(2024, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), null, "dmytro@example.com", "Дмитро Петренко", true, 2L, 5, "Ідеальний для workstation", null, null }
                });

            migrationBuilder.CreateIndex(
                name: "ix_brands_is_active",
                table: "brands",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_brands_name",
                table: "brands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_is_active_sort_order",
                table: "categories",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_categories_name",
                table: "categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_categories_parent_category_id",
                table: "categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_product_attributes_product_id_name",
                table: "product_attributes",
                columns: new[] { "ProductId", "Name" });

            migrationBuilder.CreateIndex(
                name: "ix_product_attributes_product_id_sort_order",
                table: "product_attributes",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_is_primary",
                table: "product_images",
                columns: new[] { "ProductId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id_sort_order",
                table: "product_images",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_product_id_is_approved",
                table: "product_reviews",
                columns: new[] { "ProductId", "IsApproved" });

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_rating",
                table: "product_reviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_id_is_active",
                table: "products",
                columns: new[] { "BrandId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id_is_active",
                table: "products",
                columns: new[] { "CategoryId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_products_is_featured",
                table: "products",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "ix_products_name",
                table: "products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "ix_products_price_is_active",
                table: "products",
                columns: new[] { "Price", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_products_sku",
                table: "products",
                column: "SKU",
                unique: true,
                filter: "\"SKU\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_attributes");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "product_reviews");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropTable(
                name: "categories");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WebApplication.asp.net.c3.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    ParentCategoryId = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Sku = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    StockQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsAvailable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AverageRating = table.Column<double>(type: "double precision", nullable: true),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    BrandId = table.Column<int>(type: "integer", nullable: false),
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomerEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductId = table.Column<int>(type: "integer", nullable: false),
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
                    table.ForeignKey(
                        name: "FK_product_reviews_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "brands",
                columns: new[] { "Id", "Country", "CreatedAt", "CreatedBy", "Description", "IsActive", "LogoUrl", "Name", "UpdatedAt", "UpdatedBy", "WebsiteUrl" },
                values: new object[,]
                {
                    { 1, "USA", new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(931), null, "Світовий лідер у виробництві процесорів", true, null, "Intel", null, null, null },
                    { 2, "USA", new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(932), null, "Високопродуктивні процесори та відеокарти", true, null, "AMD", null, null, null },
                    { 3, "USA", new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(934), null, "Лідер у виробництві графічних процесорів", true, null, "NVIDIA", null, null, null },
                    { 4, "Taiwan", new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(936), null, "Материнські плати та периферія", true, null, "ASUS", null, null, null },
                    { 5, "USA", new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(937), null, "Оперативна пам'ять та периферія", true, null, "Corsair", null, null, null },
                    { 6, "South Korea", new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(939), null, "Накопичувачі та електроніка", true, null, "Samsung", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "Description", "ImageUrl", "IsActive", "Name", "ParentCategoryId", "SortOrder", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(783), null, "CPU для ПК та серверів", null, true, "Процесори", null, 1, null, null },
                    { 2, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(787), null, "GPU для ігор та професійних задач", null, true, "Відеокарти", null, 2, null, null },
                    { 3, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(789), null, "Motherboards для різних платформ", null, true, "Материнські плати", null, 3, null, null },
                    { 4, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(790), null, "RAM модулі DDR4/DDR5", null, true, "Оперативна пам'ять", null, 4, null, null },
                    { 5, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(792), null, "SSD, HDD, NVMe диски", null, true, "Накопичувачі", null, 5, null, null }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "AverageRating", "BrandId", "CategoryId", "CreatedAt", "CreatedBy", "Description", "DiscountPrice", "IsAvailable", "IsFeatured", "Name", "Price", "Sku", "StockQuantity", "Tags", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, null, 1, 1, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(972), null, "24-ядерний процесор для високопродуктивних систем", null, true, true, "Intel Core i9-14900K", 25999m, "CPU-INT-I914900K", 15, null, null, null },
                    { 2, null, 2, 1, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(975), null, "16-ядерний процесор з підтримкою DDR5", 21999m, true, true, "AMD Ryzen 9 7950X", 23499m, "CPU-AMD-R97950X", 20, null, null, null },
                    { 3, null, 3, 2, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(978), null, "Топова відеокарта для 4K гемінгу", null, true, true, "NVIDIA RTX 4090", 79999m, "GPU-NVD-RTX4090", 8, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "AverageRating", "BrandId", "CategoryId", "CreatedAt", "CreatedBy", "Description", "DiscountPrice", "IsAvailable", "Name", "Price", "Sku", "StockQuantity", "Tags", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 4, null, 4, 3, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(980), null, "Материнська плата для Intel 13/14 gen", null, true, "ASUS ROG STRIX Z790-E", 15999m, "MB-ASUS-Z790E", 12, null, null, null },
                    { 5, null, 5, 4, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(982), null, "Оперативна пам'ять DDR5-6000 MHz", null, true, "Corsair Vengeance DDR5 32GB", 5499m, "RAM-COR-VEN32", 30, null, null, null }
                });

            migrationBuilder.InsertData(
                table: "products",
                columns: new[] { "Id", "AverageRating", "BrandId", "CategoryId", "CreatedAt", "CreatedBy", "Description", "DiscountPrice", "IsAvailable", "IsFeatured", "Name", "Price", "Sku", "StockQuantity", "Tags", "UpdatedAt", "UpdatedBy" },
                values: new object[] { 6, null, 6, 5, new DateTime(2025, 11, 30, 19, 32, 56, 277, DateTimeKind.Utc).AddTicks(985), null, "Швидкий NVMe SSD накопичувач", 7299m, true, true, "Samsung 990 PRO 2TB", 7999m, "SSD-SAM-990PRO2TB", 25, null, null, null });

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
                name: "ix_product_attributes_attribute_name",
                table: "product_attributes",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "ix_product_attributes_product_id",
                table: "product_attributes",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "ix_product_images_product_id",
                table: "product_images",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_product_id",
                table: "product_reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "ix_product_reviews_rating",
                table: "product_reviews",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "ix_products_brand_id",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "ix_products_category_id",
                table: "products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_products_is_available_is_featured",
                table: "products",
                columns: new[] { "IsAvailable", "IsFeatured" });

            migrationBuilder.CreateIndex(
                name: "ix_products_price",
                table: "products",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "ix_products_sku",
                table: "products",
                column: "Sku",
                unique: true);
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

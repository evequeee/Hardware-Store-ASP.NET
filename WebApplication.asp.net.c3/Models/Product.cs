using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.asp.net.c3.Models
{
    public class Product : BaseEntity
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        public int StockQuantity { get; set; } = 0;

        public bool IsAvailable { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        [Range(0.0, 5.0)]
        public double? AverageRating { get; set; }

        public int ReviewCount { get; set; } = 0;

        [MaxLength(500)]
        public string? Tags { get; set; }

        [Required]
        public long CategoryId { get; set; }

        [Required]
        public long BrandId { get; set; }

        public Category Category { get; set; } = null!;
        public Brand Brand { get; set; } = null!;
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<ProductAttribute> ProductAttributes { get; set; } = new List<ProductAttribute>();
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}

using System.ComponentModel.DataAnnotations;

namespace WebApplication.asp.net.c3.Models
{
    public class ProductImage : BaseEntity
    {
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? AltText { get; set; }
        
        public bool IsPrimary { get; set; } = false;
        
        public int SortOrder { get; set; } = 0;
        
        [Required]
        public int ProductId { get; set; }
        
        public Product Product { get; set; } = null!;
    }
}
using System.ComponentModel.DataAnnotations;

namespace WebApplication.asp.net.c3.Models
{
    public class ProductAttribute : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(500)]
        public string Value { get; set; } = string.Empty;
        
        [MaxLength(50)]
        public string? Unit { get; set; }
        
        public int SortOrder { get; set; } = 0;
        
        [Required]
        public int ProductId { get; set; }
        
        public Product Product { get; set; } = null!;
    }
}
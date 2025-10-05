using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication.asp.net.c3.Models
{
    public class ProductReview : BaseEntity
    {
        [Required]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string? CustomerEmail { get; set; }
        
        [Range(1, 5)]
        public int Rating { get; set; }
        
        [MaxLength(200)]
        public string? Title { get; set; }
        
        [MaxLength(2000)]
        public string? Comment { get; set; }
        
        public bool IsApproved { get; set; } = false;
        
        public DateTime? ApprovedAt { get; set; }
        
        [MaxLength(100)]
        public string? ApprovedBy { get; set; }
        
        [Required]
        public long ProductId { get; set; }
        
        public Product Product { get; set; } = null!;
    }
}
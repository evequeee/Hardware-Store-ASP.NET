using System.ComponentModel.DataAnnotations;

namespace WebApplication.asp.net.c3.Models
{
    public abstract class BaseEntity
    {
        public long Id { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        [MaxLength(100)]
        public string? CreatedBy { get; set; }
        
        [MaxLength(100)]
        public string? UpdatedBy { get; set; }
        
        public bool IsDeleted { get; set; } = false;
    }
}
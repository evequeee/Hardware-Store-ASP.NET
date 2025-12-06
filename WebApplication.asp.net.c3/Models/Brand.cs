using System.ComponentModel.DataAnnotations;

namespace WebApplication.asp.net.c3.Models;

public class Brand : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string? Description { get; set; }
    
    [MaxLength(300)]
    public string? LogoUrl { get; set; }
    
    [MaxLength(200)]
    public string? WebsiteUrl { get; set; }
    
    [MaxLength(100)]
    public string? Country { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
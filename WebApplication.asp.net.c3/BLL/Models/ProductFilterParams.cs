namespace WebApplication.asp.net.c3.BLL.Models;

/// <summary>
/// Product filtering parameters
/// </summary>
public class ProductFilterParams : PaginationParams
{
    /// <summary>
    /// Filter by category ID
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Filter by brand ID
    /// </summary>
    public int? BrandId { get; set; }

    /// <summary>
    /// Search term (name, description, tags)
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Minimum price
    /// </summary>
    public decimal? MinPrice { get; set; }

    /// <summary>
    /// Maximum price
    /// </summary>
    public decimal? MaxPrice { get; set; }

    /// <summary>
    /// Only in stock products
    /// </summary>
    public bool? InStock { get; set; }

    /// <summary>
    /// Only featured products
    /// </summary>
    public bool? IsFeatured { get; set; }

    /// <summary>
    /// Minimum rating (1-5)
    /// </summary>
    public double? MinRating { get; set; }

    /// <summary>
    /// Sort by field (name, price, rating, date)
    /// </summary>
    public string SortBy { get; set; } = "name";

    /// <summary>
    /// Sort order (asc, desc)
    /// </summary>
    public string SortOrder { get; set; } = "asc";
}

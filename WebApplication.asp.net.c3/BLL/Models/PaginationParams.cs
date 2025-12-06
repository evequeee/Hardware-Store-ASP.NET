namespace WebApplication.asp.net.c3.BLL.Models;

/// <summary>
/// Pagination parameters for API requests
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Calculate skip count for database query
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}

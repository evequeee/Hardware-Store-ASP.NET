namespace HardwareStore.WebUI.Caching;

/// <summary>
/// Cache key constants for consistent key naming
/// </summary>
public static class CacheKeys
{
    // Product cache keys
    public const string AllProducts = "products:all";
    public const string ProductById = "products:id:{0}";
    public const string ProductsByCategory = "products:category:{0}";
    
    // Pattern for invalidation
    public const string ProductsPattern = "products:";
    
    // Cache durations
    public static class Durations
    {
        /// <summary>
        /// Short duration for frequently changing data (2 minutes)
        /// </summary>
        public static readonly TimeSpan Short = TimeSpan.FromMinutes(2);
        
        /// <summary>
        /// Medium duration for moderately changing data (5 minutes)
        /// </summary>
        public static readonly TimeSpan Medium = TimeSpan.FromMinutes(5);
        
        /// <summary>
        /// Long duration for rarely changing data (15 minutes)
        /// </summary>
        public static readonly TimeSpan Long = TimeSpan.FromMinutes(15);
        
        /// <summary>
        /// Sliding expiration for active data (1 minute)
        /// </summary>
        public static readonly TimeSpan SlidingShort = TimeSpan.FromMinutes(1);
        
        /// <summary>
        /// Sliding expiration for reference data (3 minutes)
        /// </summary>
        public static readonly TimeSpan SlidingMedium = TimeSpan.FromMinutes(3);
    }
    
    /// <summary>
    /// Generate product by ID cache key
    /// </summary>
    public static string GetProductByIdKey(string id) => string.Format(ProductById, id);
    
    /// <summary>
    /// Generate products by category cache key
    /// </summary>
    public static string GetProductsByCategoryKey(string category) => string.Format(ProductsByCategory, category);
}

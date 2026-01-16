using HardwareStore.AggregatorService.DTOs;
using System.Text.Json;

namespace HardwareStore.AggregatorService.Services;

public class ProductsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductsClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsClient(HttpClient httpClient, ILogger<ProductsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching all products from WebAPI");
            
            var response = await _httpClient.GetAsync("/api/products", cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var products = JsonSerializer.Deserialize<List<ProductDto>>(content, _jsonOptions);

            _logger.LogInformation("Successfully fetched {Count} products", products?.Count ?? 0);
            
            return products ?? new List<ProductDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching products from WebAPI");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching product {ProductId} from WebAPI", id);
            
            var response = await _httpClient.GetAsync($"/api/products/{id}", cancellationToken);
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var product = JsonSerializer.Deserialize<ProductDto>(content, _jsonOptions);

            _logger.LogInformation("Successfully fetched product {ProductId}", id);
            
            return product;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching product {ProductId} from WebAPI", id);
            throw;
        }
    }
}

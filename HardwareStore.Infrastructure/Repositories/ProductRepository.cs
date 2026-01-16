using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Infrastructure.Data;
using MongoDB.Driver;

namespace HardwareStore.Infrastructure.Repositories;

public class ProductRepository : MongoRepository<Product>, IProductRepository
{
    public ProductRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Eq(x => x.Category, category);
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"));
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Product>> GetAvailableProductsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<Product>.Filter.And(
            Builders<Product>.Filter.Eq(x => x.IsAvailable, true),
            Builders<Product>.Filter.Gt(x => x.StockQuantity, 0)
        );
        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }
}

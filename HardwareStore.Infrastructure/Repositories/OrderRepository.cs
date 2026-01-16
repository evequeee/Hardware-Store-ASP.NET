using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Infrastructure.Data;
using MongoDB.Driver;

namespace HardwareStore.Infrastructure.Repositories;

public class OrderRepository : MongoRepository<Order>, IOrderRepository
{
    public OrderRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.Eq(x => x.CustomerId, customerId);
        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Order>> GetByStatusAsync(string status, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.Eq(x => x.Status, status);
        return await _collection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}

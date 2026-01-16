using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Infrastructure.Data;
using MongoDB.Driver;

namespace HardwareStore.Infrastructure.Repositories;

public class CustomerRepository : MongoRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(MongoDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Eq("Email", email.ToLowerInvariant());
        return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Customer>.Filter.Eq("Email", email.ToLowerInvariant());
        var count = await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        return count > 0;
    }
}

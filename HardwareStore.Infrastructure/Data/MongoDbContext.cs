using HardwareStore.Domain.Common;
using HardwareStore.Domain.ValueObjects;
using HardwareStore.Infrastructure.Data.Serializers;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HardwareStore.Infrastructure.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private static bool _isInitialized = false;
    private static readonly object _lock = new object();

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        if (!_isInitialized)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    RegisterSerializers();
                    _isInitialized = true;
                }
            }
        }

        var client = new MongoClient(settings.Value.ConnectionString);
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoDatabase Database => _database;

    public IMongoCollection<T> GetCollection<T>() where T : BaseEntity
    {
        var collectionName = GetCollectionName<T>();
        return _database.GetCollection<T>(collectionName);
    }

    private static string GetCollectionName<T>()
    {
        var attribute = (BsonCollectionAttribute?)Attribute.GetCustomAttribute(
            typeof(T), typeof(BsonCollectionAttribute));

        return attribute?.CollectionName ?? typeof(T).Name.ToLowerInvariant() + "s";
    }

    private static void RegisterSerializers()
    {
        if (!BsonClassMap.IsClassMapRegistered(typeof(Money)))
        {
            BsonSerializer.RegisterSerializer(typeof(Money), new MoneySerializer());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Address)))
        {
            BsonSerializer.RegisterSerializer(typeof(Address), new AddressSerializer());
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Email)))
        {
            BsonSerializer.RegisterSerializer(typeof(Email), new EmailSerializer());
        }
    }
}

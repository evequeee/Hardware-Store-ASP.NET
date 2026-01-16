using HardwareStore.Domain.Entities;

namespace HardwareStore.Infrastructure.Data.Seeding;

public interface IDataSeeder
{
    Task SeedAsync();
}

namespace HardwareStore.Infrastructure.Data.Seeding;

public class DatabaseSeeder
{
    private readonly IEnumerable<IDataSeeder> _seeders;

    public DatabaseSeeder(IEnumerable<IDataSeeder> seeders)
    {
        _seeders = seeders;
    }

    public async Task SeedAllAsync()
    {
        foreach (var seeder in _seeders)
        {
            await seeder.SeedAsync();
        }
    }
}

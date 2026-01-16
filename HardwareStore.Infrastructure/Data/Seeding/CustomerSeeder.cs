using HardwareStore.Domain.Entities;
using HardwareStore.Domain.Interfaces;
using HardwareStore.Domain.ValueObjects;

namespace HardwareStore.Infrastructure.Data.Seeding;

public class CustomerSeeder : IDataSeeder
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerSeeder(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task SeedAsync()
    {
        var existingCustomers = await _customerRepository.GetAllAsync();
        
        if (existingCustomers.Any())
            return;

        var customers = new List<Customer>
        {
            new Customer
            {
                FirstName = "John",
                LastName = "Doe",
                Email = new Email("john.doe@example.com"),
                Phone = "+1-555-0101",
                Address = new Address("123 Main St", "New York", "NY", "10001", "USA")
            },
            new Customer
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = new Email("jane.smith@example.com"),
                Phone = "+1-555-0102",
                Address = new Address("456 Oak Ave", "Los Angeles", "CA", "90001", "USA")
            },
            new Customer
            {
                FirstName = "Michael",
                LastName = "Johnson",
                Email = new Email("michael.johnson@example.com"),
                Phone = "+1-555-0103",
                Address = new Address("789 Pine Rd", "Chicago", "IL", "60601", "USA")
            }
        };

        foreach (var customer in customers)
        {
            await _customerRepository.CreateAsync(customer);
        }
    }
}

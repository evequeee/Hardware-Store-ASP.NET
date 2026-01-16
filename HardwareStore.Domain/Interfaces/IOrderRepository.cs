using HardwareStore.Domain.Entities;

namespace HardwareStore.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
}

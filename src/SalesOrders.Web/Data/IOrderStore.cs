using SalesOrders.Web.Models;

namespace SalesOrders.Web.Data;

/// <summary>
/// Persistence abstraction for sales orders. Implementations must make
/// <see cref="UpdateAsync{T}"/> an atomic read-modify-write.
/// </summary>
public interface IOrderStore
{
    Task<IReadOnlyList<SalesOrder>> GetAllAsync();

    /// <summary>
    /// Loads all orders, applies <paramref name="mutate"/> and persists the result.
    /// If <paramref name="mutate"/> throws, nothing is written.
    /// </summary>
    Task<T> UpdateAsync<T>(Func<List<SalesOrder>, T> mutate);
}

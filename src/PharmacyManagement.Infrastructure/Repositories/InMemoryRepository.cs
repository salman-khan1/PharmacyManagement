using PharmacyManagement.Domain.Interfaces;
using System.Linq.Expressions;

namespace PharmacyManagement.Infrastructure.Repositories;

public class InMemoryRepository<T> : IRepository<T> where T : class
{
    protected readonly List<T> _items = new();
    protected readonly object _lock = new();

    public Task<T?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            var property = typeof(T).GetProperty("Id");
            if (property == null) return Task.FromResult<T?>(null);

            var item = _items.FirstOrDefault(i => (Guid)(property.GetValue(i)!) == id);
            return Task.FromResult(item);
        }
    }

    public Task<IEnumerable<T>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_items.AsEnumerable());
        }
    }

    public Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            return Task.FromResult(_items.AsQueryable().Where(predicate).AsEnumerable());
        }
    }

    public Task<T> AddAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        lock (_lock)
        {
            _items.Add(entity);
            return Task.FromResult(entity);
        }
    }

    public Task UpdateAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        lock (_lock)
        {
            var property = typeof(T).GetProperty("Id");
            if (property == null) return Task.CompletedTask;

            var id = (Guid)property.GetValue(entity)!;
            var index = _items.FindIndex(i => (Guid)(property.GetValue(i)!) == id);
            if (index >= 0)
            {
                _items[index] = entity;
            }
            return Task.CompletedTask;
        }
    }

    public Task DeleteAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        lock (_lock)
        {
            _items.Remove(entity);
            return Task.CompletedTask;
        }
    }

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        lock (_lock)
        {
            return Task.FromResult(predicate == null ? _items.Count : _items.AsQueryable().Count(predicate));
        }
    }

    public Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        lock (_lock)
        {
            return Task.FromResult(_items.AsQueryable().Any(predicate));
        }
    }
}

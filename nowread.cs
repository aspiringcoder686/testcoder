using System.Linq.Expressions;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(long id);
    Task<T?> GetByIdAsync(Guid id);

    Task<T?> GetByPredicateAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);

    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);

    Task<int> SaveChangesAsync();
}
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        if (id == null)
            throw new ArgumentNullException(nameof(id));

        return await _dbSet.FindAsync(id);
    }

    public Task<T?> GetByIdAsync(int id) => GetByIdAsync((object)id);
    public Task<T?> GetByIdAsync(long id) => GetByIdAsync((object)id);
    public Task<T?> GetByIdAsync(Guid id) => GetByIdAsync((object)id);

    public async Task<T?> GetByPredicateAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}


public class CustomerService
{
    private readonly IGenericRepository<Customer> _customerRepo;

    public CustomerService(IGenericRepository<Customer> customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public async Task<Customer?> GetCustomerAsync(Guid id)
    {
        return await _customerRepo.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomers()
    {
        return await _customerRepo.FindAsync(c => c.IsActive);
    }

    //var user = await _repo.GetByPredicateAsync(u => u.Id == 5);

}

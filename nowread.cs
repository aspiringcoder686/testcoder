public interface IRepository<T> where T : class
{
    Task<T> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task DeleteAsync(T entity);
    Task SaveChangesAsync();
}


public class Repository<T> : IRepository<T> where T : class
{
    protected readonly YourDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(YourDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public Task<T> GetByIdAsync(int id) => _dbSet.FindAsync(id).AsTask();
    public Task<IEnumerable<T>> GetAllAsync() => Task.FromResult(_dbSet.AsEnumerable());
    public Task AddAsync(T entity) => _dbSet.AddAsync(entity).AsTask();
    public Task DeleteAsync(T entity)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}


public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetHighValueCustomersAsync();
}


public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(YourDbContext context) : base(context) { }

    public async Task<IEnumerable<Customer>> GetHighValueCustomersAsync()
    {
        return await _dbSet.Where(c => c.TotalValue > 100000).ToListAsync();
    }
}


public class CustomerService
{
    private readonly ICustomerRepository _customerRepo;

    public CustomerService(ICustomerRepository customerRepo)
    {
        _customerRepo = customerRepo;
    }

    public Task<IEnumerable<Customer>> GetHighValueCustomersAsync()
    {
        return _customerRepo.GetHighValueCustomersAsync();
    }

    public Task AddCustomerAsync(Customer customer)
    {
        return _customerRepo.AddAsync(customer);
    }
}

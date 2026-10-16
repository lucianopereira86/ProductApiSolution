using Microsoft.EntityFrameworkCore;

public class EfProductRepository(AppDbContext context) : IProductRepository
{
    private readonly AppDbContext _context = context;

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _context.Products.Add(product);
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Products.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public Task<Product?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        return _context.Products.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
    }

    public void SetOriginalRowVersion(Product product, byte[] rowVersion)
    {
        _context.Entry(product)
            .Property(p => p.RowVersion)
            .OriginalValue = rowVersion;
    }
}
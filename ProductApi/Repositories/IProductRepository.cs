public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<Product?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Product product, CancellationToken cancellationToken);
    void SetOriginalRowVersion(Product product, byte[] rowVersion);
}
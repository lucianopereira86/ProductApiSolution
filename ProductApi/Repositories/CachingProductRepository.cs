public class CachingProductRepository(IProductRepository inner, ILogger<CachingProductRepository> logger) : IProductRepository
{
    private readonly Dictionary<int, Product> _products = [];
    private readonly IProductRepository _inner = inner;
    private readonly ILogger<CachingProductRepository> _logger = logger;

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await _inner.AddAsync(product, cancellationToken);
        _products.TryAdd(product.Id, product);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        if (_products.TryGetValue(id, out var product))
        {
            _logger.LogInformation("Cache HIT for Product {ProductId}", id);
            return product;
        }
        else
        {
            _logger.LogInformation("Cache MISS for Product {ProductId}", id);
            product = await _inner.GetByIdAsync(id, cancellationToken);
            if (product is not null)
            {
                _products.Add(id, product);
            }
            return product;
        }
    }

    public Task<Product?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        return _inner.GetByIdForUpdateAsync(id, cancellationToken);
    }

    public void SetOriginalRowVersion(Product product, byte[] rowVersion)
    {
        _inner.SetOriginalRowVersion(product, rowVersion);
    }
}
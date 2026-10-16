public class LoggingProductRepository(IProductRepository inner, ILogger<LoggingProductRepository> logger) : IProductRepository
{
    private readonly IProductRepository _inner = inner;
    private readonly ILogger<LoggingProductRepository> _logger = logger;

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executando AddAsync...");
        await _inner.AddAsync(product, cancellationToken);
        _logger.LogInformation("AddAsync executado.");
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executando GetByIdAsync...");
        var product = await _inner.GetByIdAsync(id, cancellationToken);
        if (product is null)
            _logger.LogInformation("Product not found.");
        else
            _logger.LogInformation("Product {ProductId} found.", product.Id);

        return product;
    }

    public async Task<Product?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Executando GetByIdAsync...");
        var product = await _inner.GetByIdForUpdateAsync(id, cancellationToken);
        if (product is null)
            _logger.LogInformation("Product not found.");
        else
            _logger.LogInformation("Product {ProductId} found.", product.Id);

        return product;
    }

    public void SetOriginalRowVersion(Product product, byte[] rowVersion)
    {
        _logger.LogInformation(
            "Setting original RowVersion for Product {ProductId}",
            product.Id);

        _inner.SetOriginalRowVersion(product, rowVersion);
    }
}
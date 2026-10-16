public class ProductService(IProductRepository productRepository)
{
    private readonly IProductRepository _productRepository = productRepository;

    public async Task<Product> CreateAsync(string createdBy, string name, decimal price, int stock, CancellationToken cancellationToken)
    {
        var product = new Product(createdBy, name, price, stock);
        await _productRepository.AddAsync(product, cancellationToken);
        return product;
    }
    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken) => _productRepository.GetByIdAsync(id, cancellationToken);
}
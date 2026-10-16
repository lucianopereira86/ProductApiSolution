public class GetProductByIdHandler(IProductRepository productRepository) : IRequestHandler<GetProductByIdQuery, Product?>
{
    private readonly IProductRepository _productRepository = productRepository;

    public Task<Product?> HandleAsync(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return _productRepository.GetByIdAsync(request.Id, cancellationToken);
    }
}
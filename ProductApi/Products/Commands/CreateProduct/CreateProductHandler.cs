public class CreateProductHandler(IProductRepository productRepository, IUnitOfWork unitOfWork): IRequestHandler<CreateProductCommand, int>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<int> HandleAsync(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var product = new Product(request.CreatedBy, request.Name, request.Price, request.Stock, request.Description);
        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return product.Id;
    }
}
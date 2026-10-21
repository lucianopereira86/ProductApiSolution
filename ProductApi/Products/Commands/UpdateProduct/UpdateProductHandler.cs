using System.Buffers.Text;
using Microsoft.AspNetCore.Authorization;

public class UpdateProductHandler(IAuthorizationService authorizationService, ICurrentUserService currentUserService, IProductRepository productRepository, IUnitOfWork unitOfWork) : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<bool> HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {

        var product = await _productRepository.GetByIdForUpdateAsync(command.Id, cancellationToken);
        if (product is null)
            return false;

        var authorizationResult = await _authorizationService.AuthorizeAsync(_currentUserService.User ?? throw new InvalidOperationException("User not found."), product, "Products.Owner");
        if (!authorizationResult.Succeeded)
            throw new ForbiddenException("Product can only be modified by its owner.");

        if (!Base64.IsValid(command.RowVersion))
            throw new ArgumentException("Row version is not a valid Base64 string.");

        var originalRowVersion = Convert.FromBase64String(command.RowVersion);
        if (originalRowVersion.Length != 8)
            throw new ArgumentException("RowVersion must contain exactly 8 bytes.");

        _productRepository.SetOriginalRowVersion(product, originalRowVersion);

        product.UpdateDetails(command.Name, command.Price, command.Stock, command.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.VisualBasic;
using Moq;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

public class UpdateProductHandlerTests
{
    [Fact]
    public async Task NonOwner_ShouldThrowForbiddenException_AndNotSaveChanges()
    {
        // Arrange
        const string ownerId = "owner-123";
        const string currentUserId = "user-456";

        var identity = new ClaimsIdentity([new Claim("sub", currentUserId)], authenticationType: "Test");

        var currentUser = new ClaimsPrincipal(identity);

        var command = new UpdateProductCommand(1, "product 1", 1, 1, null, "abc");

        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), "Products.Owner"))
            .ReturnsAsync(AuthorizationResult.Failed());

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock
            .Setup(s => s.UserId)
            .Returns(currentUserId);
        currentUserServiceMock
            .Setup(s => s.User)
            .Returns(currentUser);

        var product = new Product(ownerId, command.Name, command.Price, command.Stock, command.Description);
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(s => s.GetByIdForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var handler = new UpdateProductHandler(authorizationServiceMock.Object, currentUserServiceMock.Object, productRepositoryMock.Object, unitOfWorkMock.Object);

        // Act
        await Assert.ThrowsAsync<ForbiddenException>(async () => await handler.HandleAsync(command, CancellationToken.None));

        // Assert
        authorizationServiceMock.Verify(v => v.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.Is<object>(resource => ReferenceEquals(resource, product)), "Products.Owner"), Times.Once);
        unitOfWorkMock.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Owner_ShouldUpdateProduct_AndSaveChanges()
    {
        // Arrange
        const string ownerId = "owner-123";
        const string currentUserId = "owner-123";
        var rowVersion = new byte[8];
        var rowVersionBase64 = Convert.ToBase64String(rowVersion);
        var updatedProduct = new
        {
            Id = 1,
            Name = "Updated product",
            Price = 150,
            Stock = 5,
            Description = "Updated description"
        };

        var identity = new ClaimsIdentity([new Claim("sub", currentUserId)], authenticationType: "Test");

        var currentUser = new ClaimsPrincipal(identity);

        var command = new UpdateProductCommand(updatedProduct.Id, updatedProduct.Name, updatedProduct.Price, updatedProduct.Stock, updatedProduct.Description, rowVersionBase64);

        var authorizationServiceMock = new Mock<IAuthorizationService>();
        authorizationServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), "Products.Owner"))
            .ReturnsAsync(AuthorizationResult.Success());

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock
            .Setup(s => s.UserId)
            .Returns(currentUserId);
        currentUserServiceMock
            .Setup(s => s.User)
            .Returns(currentUser);

        var product = new Product(
            ownerId,
            "Original product",
            100,
            10,
            "Original description");
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock
            .Setup(s => s.GetByIdForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var handler = new UpdateProductHandler(authorizationServiceMock.Object, currentUserServiceMock.Object, productRepositoryMock.Object, unitOfWorkMock.Object);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        authorizationServiceMock.Verify(v => v.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.Is<object>(resource => ReferenceEquals(resource, product)), "Products.Owner"), Times.Once);
        productRepositoryMock.Verify(r => r.SetOriginalRowVersion(product, It.Is<byte[]>(version => version.SequenceEqual(rowVersion))), Times.Once);
        Assert.Equal(updatedProduct.Name, product.Name);
        Assert.Equal(updatedProduct.Price, product.Price);
        Assert.Equal(updatedProduct.Stock, product.Stock);
        Assert.Equal(updatedProduct.Description, product.Description);
        unitOfWorkMock.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProductNotFound_ShouldReturnFalse_AndNotSaveChanges()
    {
        // Arrange
        var rowVersion = Convert.ToBase64String(new byte[8]);

        var command = new UpdateProductCommand(
            1,
            "Updated product",
            150,
            5,
            "Updated description",
            rowVersion);
        var authorizationServiceMock = new Mock<IAuthorizationService>();
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        var productRepositoryMock = new Mock<IProductRepository>();
        productRepositoryMock.Setup(s => s.GetByIdForUpdateAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((Product?) null);
        var unitOfWorkMock = new Mock<IUnitOfWork>();

        var handler = new UpdateProductHandler(authorizationServiceMock.Object, currentUserServiceMock.Object, productRepositoryMock.Object, unitOfWorkMock.Object);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.False(result);
        productRepositoryMock.Verify(v => v.GetByIdForUpdateAsync(command.Id, It.IsAny<CancellationToken>()), Times.Once);
        authorizationServiceMock.Verify(v => v.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), "Products.Owner"), Times.Never);
        productRepositoryMock.Verify(r => r.SetOriginalRowVersion(It.IsAny<Product>(), It.IsAny<byte[]>()), Times.Never);
        unitOfWorkMock.Verify(v => v.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
using ProductApi.Products.Commands.UpdateProduct;

public class UpdateProductCommandTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void UpdateProductCommand_WithInvalidName_ShouldBeInvalid(string? name)
    {
        // Arrange
        var updatedDetails = new
        {
            Id = 1,
            Name = name,
            Price = 1,
            Stock = 1,
            Description = "Updated description",
            RowVersion = Convert.ToBase64String(new byte[8])
        };

        var validator = new UpdateProductCommandValidator();
        
        // Act
        var results = validator.Validate(new UpdateProductCommand(
            updatedDetails.Id,
            updatedDetails.Name!,
            updatedDetails.Price,
            updatedDetails.Stock,
            updatedDetails.Description,
            updatedDetails.RowVersion));

        // Assert
        Assert.False(results.IsValid);
        Assert.Single(results.Errors);
        Assert.Equal(nameof(Product.Name), results.Errors.First().PropertyName);
    }


    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void UpdateProductCommand_WithInvalidPrice_ShouldBeInvalid(decimal price)
    {
        // Arrange
        var updatedDetails = new
        {
            Id = 1,
            Name = "Updated name",
            Price = price,
            Stock = 1,
            Description = "Updated description",
            RowVersion = Convert.ToBase64String(new byte[8])
        };

        var validator = new UpdateProductCommandValidator();

        // Act
        var results = validator.Validate(new UpdateProductCommand(
            updatedDetails.Id,
            updatedDetails.Name,
            updatedDetails.Price,
            updatedDetails.Stock,
            updatedDetails.Description,
            updatedDetails.RowVersion));

        // Assert
        Assert.False(results.IsValid);
        Assert.Single(results.Errors);
        Assert.Equal(nameof(Product.Price), results.Errors.First().PropertyName);
    }


    [Theory]
    [InlineData(-1)]
    public void UpdateProductCommand_WithInvalidStock_ShouldBeInvalid(int stock)
    {
        // Arrange
        var updatedDetails = new
        {
            Id = 1,
            Name = "Updated name",
            Price = 10m,
            Stock = stock,
            Description = "Updated description",
            RowVersion = Convert.ToBase64String(new byte[8])
        };

        var validator = new UpdateProductCommandValidator();

        // Act
        var results = validator.Validate(new UpdateProductCommand(
            updatedDetails.Id,
            updatedDetails.Name,
            updatedDetails.Price,
            updatedDetails.Stock,
            updatedDetails.Description,
            updatedDetails.RowVersion));

        // Assert
        Assert.False(results.IsValid);
        Assert.Single(results.Errors);
        Assert.Equal(nameof(Product.Stock), results.Errors.First().PropertyName);
    }


    [Fact]
    public void UpdateProductCommand_WithInvalidValues_ShouldBeInvalid()
    {
        // Arrange
        var updatedDetails = new
        {
            Id = 1,
            Name = string.Empty,
            Price=  0,
            Stock = -1,
            Description = "Updated description",
            RowVersion = Convert.ToBase64String(new byte[8])
        };

        var validator = new UpdateProductCommandValidator();

        // Act
        var results = validator.Validate(new UpdateProductCommand(
            updatedDetails.Id,
            updatedDetails.Name,
            updatedDetails.Price,
            updatedDetails.Stock,
            updatedDetails.Description,
            updatedDetails.RowVersion));

        // Assert
        Assert.False(results.IsValid);
        Assert.Equal(3, results.Errors.Count);
        Assert.NotNull(results.Errors.FirstOrDefault(s => s.PropertyName == nameof(Product.Name)));
        Assert.NotNull(results.Errors.FirstOrDefault(s => s.PropertyName == nameof(Product.Price)));
        Assert.NotNull(results.Errors.FirstOrDefault(s => s.PropertyName == nameof(Product.Stock)));
    }


    [Fact]
    public void UpdateProductCommand_WithValidValues_ShouldBeValid()
    {
        // Arrange
        var updatedDetails = new
        {
            Id = 1,
            Name = "Updated product",
            Price = 1,
            Stock = 1,
            Description = "Updated description",
            RowVersion = Convert.ToBase64String(new byte[8])
        };

        var validator = new UpdateProductCommandValidator();

        // Act
        var results = validator.Validate(new UpdateProductCommand(
            updatedDetails.Id,
            updatedDetails.Name,
            updatedDetails.Price,
            updatedDetails.Stock,
            updatedDetails.Description,
            updatedDetails.RowVersion));

        // Assert
        Assert.True(results.IsValid);
        Assert.Empty(results.Errors);
    }
}
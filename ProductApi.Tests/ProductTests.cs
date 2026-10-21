public class ProductTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData(" ")]
    public void UpdateDetails_WithInvalidName_ShouldNotModifyProduct(string? name)
    {
        // Arrange
        var originalDetails = new
        {
            Name = "Original",
            Price = 10m,
            Stock = 5,
            Description = "Original description"
        };
        var updatedDetails = new
        {
            Name = name,
            Price = 20m,
            Stock = 1,
            Description = "Updated description"
        };
        var product = new Product("user-123", originalDetails.Name, originalDetails.Price, originalDetails.Stock, originalDetails.Description);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => product.UpdateDetails(updatedDetails.Name!, updatedDetails.Price, updatedDetails.Stock, updatedDetails.Description));
        Assert.Equal("name", exception.ParamName);
        Assert.Equal(originalDetails.Name, product.Name);
        Assert.Equal(originalDetails.Price, product.Price);
        Assert.Equal(originalDetails.Stock, product.Stock);
        Assert.Equal(originalDetails.Description, product.Description);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void UpdateDetails_WithInvalidPrice_ShouldNotModifyProduct(decimal price)
    {
        // Arrange
        var originalDetails = new
        {
            Name = "Original",
            Price = 10m,
            Stock = 5,
            Description = "Original description"
        };
        var updatedDetails = new
        {
            Name = "Updated",
            Price = price,
            Stock = 1,
            Description = "Updated description"
        };
        var product = new Product("user-123", originalDetails.Name, originalDetails.Price, originalDetails.Stock, originalDetails.Description);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateDetails(updatedDetails.Name, updatedDetails.Price, updatedDetails.Stock, updatedDetails.Description));
        Assert.Equal("price", exception.ParamName);
        Assert.Equal(originalDetails.Name, product.Name);
        Assert.Equal(originalDetails.Price, product.Price);
        Assert.Equal(originalDetails.Stock, product.Stock);
        Assert.Equal(originalDetails.Description, product.Description);
    }

    [Fact]
    public void UpdateDetails_WithInvalidStock_ShouldNotModifyProduct()
    {
        // Arrange
        var originalDetails = new
        {
            Name = "Original",
            Price = 10m,
            Stock = 5,
            Description = "Original description"
        };
        var updatedDetails = new
        {
            Name = "Updated",
            Price = 20m,
            Stock = -1,
            Description = "Updated description"
        };
        var product = new Product("user-123", originalDetails.Name, originalDetails.Price, originalDetails.Stock, originalDetails.Description);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => product.UpdateDetails(updatedDetails.Name, updatedDetails.Price, updatedDetails.Stock, updatedDetails.Description));
        Assert.Equal("stock", exception.ParamName);
        Assert.Equal(originalDetails.Name, product.Name);
        Assert.Equal(originalDetails.Price, product.Price);
        Assert.Equal(originalDetails.Stock, product.Stock);
        Assert.Equal(originalDetails.Description, product.Description);
    }

    [Fact]
    public void UpdateDetails_WithValidData_ShouldUpdateAllProperties()
    {
        // Arrange
        var originalDetails = new
        {
            Name = "Original",
            Price = 10m,
            Stock = 5,
            Description = "Original description"
        };
        var updatedDetails = new
        {
            Name = "Updated",
            Price = 20m,
            Stock = 1,
            Description = "Updated description"
        };
        var product = new Product("user-123", originalDetails.Name, originalDetails.Price, originalDetails.Stock, originalDetails.Description);

        // Act
        product.UpdateDetails(updatedDetails.Name, updatedDetails.Price, updatedDetails.Stock, updatedDetails.Description);

        // Assert
        Assert.Equal(updatedDetails.Name, product.Name);
        Assert.Equal(updatedDetails.Price, product.Price);
        Assert.Equal(updatedDetails.Stock, product.Stock);
        Assert.Equal(updatedDetails.Description, product.Description);
    }
}
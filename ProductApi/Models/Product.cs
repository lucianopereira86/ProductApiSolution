public class Product : IEquatable<Product>
{
    public Product(string createdBy, string name, decimal price, int stock, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentNullException(nameof(createdBy), "CreatedBy must be informed.");

        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock), stock, "Initial stock must not be negative.");

        CreatedBy = createdBy;
        ChangeName(name);
        ChangePrice(price);
        Stock = stock;
        Description = description;
    }
    public string CreatedBy { get; private set; } = string.Empty;
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public string? Description { get; private set; }
    public int Stock { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    #region Equatable

    public bool Equals(Product? other)
    {
        return other is not null && Id == other.Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is Product other && Equals(other);
    }

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Product? left, Product? right)
    {
        if (left is null || right is null)
            return left is null && right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Product? left, Product? right)
    {
        return !(left == right);
    }

    #endregion

    public void ChangeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        Name = name;
    }

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), newPrice, $"Price must be greater than zero.");
        Price = newPrice;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, $"Quantity must be greater than zero.");

        Stock += quantity;
    }

    public void RemoveStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, $"Quantity must be greater than zero.");

        if (quantity > Stock)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, $"Insufficient stock ({Stock}) to be removed.");

        Stock -= quantity;
    }

    public void SetStock(int stock)
    {
        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock), stock, $"Stock must not be negative.");
        Stock = stock;
    }

    public void ChangeDescription(string? description)
    {
        Description = description;
    }

    public void UpdateDetails(string name, decimal price, int stock, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required", nameof(name));
        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price), price, $"Price must be greater than zero.");
        if (stock < 0)
            throw new ArgumentOutOfRangeException(nameof(stock), stock, $"Stock must not be negative.");

        Name = name;
        Price = price;
        Stock = stock;
        Description = description;
    }
}
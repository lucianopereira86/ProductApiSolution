public record CreateProductRequest(
    string Name,
    decimal Price,
    int Stock,
    string? Description);
public record CreateProductCommand(
    string CreatedBy,
    string Name,
    decimal Price,
    int Stock,
    string? Description)
    : IRequest<int>;
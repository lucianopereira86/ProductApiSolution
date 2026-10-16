public record UpdateProductCommand(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    string? Description,
    string RowVersion
)
    : IRequest<bool>;
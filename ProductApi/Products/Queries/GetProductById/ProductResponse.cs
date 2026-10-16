 public record ProductResponse(
    int Id,
    string Name,
    decimal Price,
    int Stock,
    string? Description,
    string RowVersion);
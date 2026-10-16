public class DisposableService : IDisposable
{
    public Guid Id { get; } = Guid.NewGuid();
    public void Dispose()
    {
        Console.WriteLine($"Disposed Id: {Id}");
    }
}
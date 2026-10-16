public class SingletonConsumer
{
    private readonly ScopedService _service;

    public SingletonConsumer(ScopedService service)
    {
        _service = service;
    }
}
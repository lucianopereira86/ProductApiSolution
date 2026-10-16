public class DisposalConsumer
{
    private readonly DisposableService _service;

    public DisposalConsumer(DisposableService service)
    {
        _service = service;
    }

    public Guid ServiceId => _service.Id;
}
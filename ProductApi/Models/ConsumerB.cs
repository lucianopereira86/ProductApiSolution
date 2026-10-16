public class ConsumerB
{
    private readonly TransientService transient;
    private readonly ScopedService scoped;
    private readonly SingletonService singleton;

    public ConsumerB(
        TransientService transient,
        ScopedService scoped,
        SingletonService singleton)
    {
        this.transient = transient;
        this.scoped = scoped;
        this.singleton = singleton;
    }

    public Guid TransientId => transient.Id;
    public Guid ScopedId => scoped.Id;
    public Guid SingletonId => singleton.Id;
}
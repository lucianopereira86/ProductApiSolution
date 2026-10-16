public abstract class PaymentHandlerBase : IPaymentHandler
{
    private IPaymentHandler? _next;

    public virtual Task HandleAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        if (_next is not null)
        {
            return _next.HandleAsync(request, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public IPaymentHandler SetNext(IPaymentHandler handler)
    {
        _next = handler;
        return handler;
    }
}
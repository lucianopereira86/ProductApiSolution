public interface IPaymentHandler
{
    IPaymentHandler SetNext(IPaymentHandler handler);

    Task HandleAsync(PaymentRequest request, CancellationToken cancellationToken);
}
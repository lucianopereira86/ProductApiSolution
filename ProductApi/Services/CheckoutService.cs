public class CheckoutService(IPaymentGateway gateway)
{
    private readonly IPaymentGateway _gateway = gateway;
    public Task ProcessPaymentAsync(decimal amount, CancellationToken cancellationToken)
    {
        return _gateway.ProcessPaymentAsync(amount, cancellationToken);
    }
}
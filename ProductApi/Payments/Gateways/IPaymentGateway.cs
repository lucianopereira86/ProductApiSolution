public interface IPaymentGateway
{
    Task ProcessPaymentAsync(decimal amount, CancellationToken cancellationToken);
}
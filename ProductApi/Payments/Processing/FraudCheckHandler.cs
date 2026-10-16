public class FraudCheckHandler : PaymentHandlerBase
{
    public override Task HandleAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("FraudCheckHandler");
        if(request.Amount > 10_000)
            throw new ArgumentOutOfRangeException(nameof(request.Amount));

        return base.HandleAsync(request, cancellationToken);
    }
}
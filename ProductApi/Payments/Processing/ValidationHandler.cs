public class ValidationHandler : PaymentHandlerBase
{
    public override Task HandleAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("ValidationHandler");
        if(request.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(request.Amount));

        return base.HandleAsync(request, cancellationToken);
    }
}
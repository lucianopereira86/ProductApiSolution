public class PaymentExecutionHandler(PaymentStrategyResolver resolver) : PaymentHandlerBase
{
    private readonly PaymentStrategyResolver _resolver = resolver;
    public override Task HandleAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("PaymentExecutionHandler");
        var strategy = _resolver.Resolve(request.Method);
        var total = new PaymentProcessor(strategy).Process(request.Amount);
        Console.WriteLine($"Total: {total}");
        return Task.CompletedTask;
    }
}
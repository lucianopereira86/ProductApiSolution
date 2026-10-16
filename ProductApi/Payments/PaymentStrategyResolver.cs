using System.Linq;

public class PaymentStrategyResolver(IEnumerable<IPaymentStrategy> strategies)
{
    private readonly IEnumerable<IPaymentStrategy> _strategies = strategies;

    public IPaymentStrategy Resolve(PaymentMethod method)
    {
        var strategy = _strategies.FirstOrDefault(f => f.Method == method);
        if(strategy is null)
            throw new ArgumentOutOfRangeException(nameof(method), method, "Method not found");
        return strategy;
    }
}
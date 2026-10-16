public class PaymentProcessor(IPaymentStrategy payment)
{
    private readonly IPaymentStrategy _strategy = payment;

    public decimal Process(decimal amount)
    {
        if(amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");

        var fee = _strategy.CalculateFee(amount);
        return amount + fee;
    }
}
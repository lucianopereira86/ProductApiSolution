public class CreditCardPayment(decimal amount) : Payment(amount)
{
    public override decimal CalculateFee()
    {
        return Amount * 0.03m;
    }
}
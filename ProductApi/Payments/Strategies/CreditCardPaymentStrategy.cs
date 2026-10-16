public class CreditCardPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.CreditCard;
    public decimal CalculateFee(decimal amount)
    {
        return amount * 0.03m;
    }
}
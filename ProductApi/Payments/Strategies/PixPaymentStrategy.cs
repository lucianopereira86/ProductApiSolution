public class PixPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Pix;
    public decimal CalculateFee(decimal amount)
    {
        return 0;
    }
}
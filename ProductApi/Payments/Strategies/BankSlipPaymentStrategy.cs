public class BankSlipPaymentStrategy : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.BankSlip;

    public decimal CalculateFee(decimal amount)
    {
        return 2m;
    }
}
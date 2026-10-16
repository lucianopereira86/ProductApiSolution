public class BankSlipPayment(decimal amount) : Payment(amount)
{
    public override decimal CalculateFee()
    {
        return 2m;
    }
}
public class PixPayment(decimal amount) : Payment(amount)
{
    public override decimal CalculateFee()
    {
        return 0m;
    }
}
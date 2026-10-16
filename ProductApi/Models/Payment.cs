public abstract class Payment
{
    protected Payment(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be greater than zero.");

        Amount = amount;
    }
    public decimal Amount { get; }
    public abstract decimal CalculateFee();
    public decimal GetTotal() { 
        return Amount + CalculateFee(); 
    }
}
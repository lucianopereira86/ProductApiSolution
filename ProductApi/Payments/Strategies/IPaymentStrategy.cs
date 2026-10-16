public interface IPaymentStrategy
{
    PaymentMethod Method { get; }
    decimal CalculateFee(decimal amount);
}
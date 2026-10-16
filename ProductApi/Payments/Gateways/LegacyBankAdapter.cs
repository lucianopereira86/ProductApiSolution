public class LegacyBankAdapter(LegacyBankSdk sdk): IPaymentGateway
{
    private readonly LegacyBankSdk _sdk = sdk;

    public Task ProcessPaymentAsync(decimal amount, CancellationToken cancellationToken)
    {
        var result = _sdk.ExecuteTransaction((double) amount);
        if(!result)
            throw new InvalidOperationException("Legacy bank transaction returned false.");
        return Task.CompletedTask;
    }
}
public class LegacyBankSdk
{
    public bool ExecuteTransaction(double value)
    {
        if(value < 0)
            return false;
            
        Console.WriteLine(
            $"Legacy transaction: {value}");

        return true;
    }
}
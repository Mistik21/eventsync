namespace eventsynс.Domain.Entities;

public class Cost
{
    public Guid Id { get; }
    public string Title {get; }
    public int  Amount {get; private set;}

    public Cost(Guid id, string title, int amount)
    {
        Id = id;
        Title = title;
        ChangeAmount(amount);
    }

    public void ChangeAmount(int amount)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException();
        Amount = amount;
    }
}
